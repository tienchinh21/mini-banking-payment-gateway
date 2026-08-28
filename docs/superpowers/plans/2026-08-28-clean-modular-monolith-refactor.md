# Clean Modular Monolith Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the .NET 8 backend from god handlers and inline route pollution in `Program.cs` to a clean, decoupled Modular Monolith with Vertical Slice routing, MediatR Pipeline Behaviors, and dedicated Domain Services.

**Architecture:** Vertical Slice per Module with strict boundary isolation. Cross-cutting concerns (DB Transactions, Logging) are handled via MediatR Pipeline Behaviors. Modules communicate via domain contracts (`IAccountLockService`, `ILedgerPostingService`, `IIdempotencyService`). Endpoints are declared per module in `Endpoints/` extension methods.

**Tech Stack:** .NET 8, ASP.NET Core Minimal APIs, MediatR 12, Entity Framework Core 8, PostgreSQL (Npgsql), xUnit.

**Spec:** `docs/superpowers/specs/2026-08-28-clean-modular-monolith-refactor-design.md`

## Global Constraints

- 0 build warnings, 0 build errors (`dotnet build be/MiniBanking.sln`).
- All existing tests in `be/tests/MiniBanking.Tests/` must pass.
- No business logic or direct route mappings in `Program.cs`.
- Handlers must be slim orchestrators (< 50 lines).

---

### Task 1: SharedKernel Contracts & Pipeline Behaviors

**Files:**
- Create: `be/src/MiniBanking.Api/SharedKernel/Behaviors/ITransactionalRequest.cs`
- Create: `be/src/MiniBanking.Api/SharedKernel/Behaviors/TransactionBehavior.cs`
- Create: `be/src/MiniBanking.Api/SharedKernel/Contracts/IAccountLockService.cs`
- Create: `be/src/MiniBanking.Api/SharedKernel/Contracts/ILedgerPostingService.cs`
- Create: `be/src/MiniBanking.Api/SharedKernel/Contracts/IIdempotencyService.cs`

**Interfaces:**
- Produces:
  - `ITransactionalRequest`: Marker interface for MediatR requests requiring database transactions.
  - `TransactionBehavior<TRequest, TResponse>`: Pipeline behavior that wraps execution in EF Core transaction.
  - `IAccountLockService`: `Task<(bool IsSuccess, string? ErrorCode)> DebitWalletAsync(Guid walletId, Money amount, CancellationToken ct)`
  - `ILedgerPostingService`: `Task<LedgerTransaction> PostDirectDebitAsync(Guid walletId, Money amount, string description, CancellationToken ct)`
  - `IIdempotencyService`: `Task<T?> CheckAsync<T>(string merchantId, string key, string bodyHash, CancellationToken ct)` & `Task SaveAsync<T>(...)`

- [ ] **Step 1: Create ITransactionalRequest and TransactionBehavior**
- [ ] **Step 2: Create Service Contracts in SharedKernel/Contracts**
- [ ] **Step 3: Register TransactionBehavior in Program.cs MediatR config**
- [ ] **Step 4: Build to verify compilation**
  Run: `dotnet build be/MiniBanking.sln`
- [ ] **Step 5: Commit changes**
  Run: `git commit -am "feat(shared): add transaction behavior and service contracts"`

---

### Task 2: Implement Domain & Application Services

**Files:**
- Create: `be/src/MiniBanking.Api/Modules/Accounts/Application/Services/AccountLockService.cs`
- Create: `be/src/MiniBanking.Api/Modules/Ledger/Application/Services/LedgerPostingService.cs`
- Create: `be/src/MiniBanking.Api/Modules/Payments/Application/Services/IdempotencyService.cs`

**Interfaces:**
- Consumes: Contracts from Task 1, `MiniBankingDbContext`, `BalanceSnapshot`, `LedgerTransaction`.
- Produces: Concrete implementations of `IAccountLockService`, `ILedgerPostingService`, `IIdempotencyService`.

- [ ] **Step 1: Write unit tests for LedgerPostingService (asserting Debit == Credit invariant)**
- [ ] **Step 2: Implement AccountLockService with `SELECT FOR UPDATE` and balance debit/credit**
- [ ] **Step 3: Implement LedgerPostingService with balanced journal entries**
- [ ] **Step 4: Implement IdempotencyService with SHA256 body hashing and replay detection**
- [ ] **Step 5: Register services in Dependency Injection (`Program.cs`)**
- [ ] **Step 6: Run tests and verify passing**
  Run: `dotnet test be/MiniBanking.sln`
- [ ] **Step 7: Commit changes**
  Run: `git commit -am "feat(services): implement AccountLock, LedgerPosting, and Idempotency services"`

---

### Task 3: Refactor Payment Handlers to Slim Orchestrators

**Files:**
- Modify: `be/src/MiniBanking.Api/Modules/Payments/Application/CreatePayment.cs`
- Modify: `be/src/MiniBanking.Api/Modules/Payments/Application/CreateRefund.cs`
- Modify: `be/src/MiniBanking.Api/Modules/Payments/Application/CreateSettlement.cs`

**Interfaces:**
- Consumes: `IAccountLockService`, `ILedgerPostingService`, `IIdempotencyService`, `ITransactionalRequest`.
- Produces: Slim Handlers (< 50 lines) implementing `IRequestHandler<TCommand, TResponse>`.

- [ ] **Step 1: Mark CreatePaymentCommand, CreateRefundCommand, CreateSettlementCommand with `ITransactionalRequest`**
- [ ] **Step 2: Refactor CreatePaymentHandler to delegate locking, ledger posting, and idempotency to services**
- [ ] **Step 3: Refactor CreateRefundHandler to delegate ledger reversal and balance credit to services**
- [ ] **Step 4: Refactor CreateSettlementHandler to delegate clearing settlement**
- [ ] **Step 5: Run tests and verify passing**
  Run: `dotnet test be/MiniBanking.sln`
- [ ] **Step 6: Commit changes**
  Run: `git commit -am "refactor(payments): slim down payment, refund, and settlement handlers"`

---

### Task 4: Extract Endpoints to Modules & Clean Program.cs

**Files:**
- Create: `be/src/MiniBanking.Api/Modules/Payments/Endpoints/PaymentEndpoints.cs`
- Create: `be/src/MiniBanking.Api/Modules/Accounts/Endpoints/AccountEndpoints.cs`
- Create: `be/src/MiniBanking.Api/Modules/Ledger/Endpoints/LedgerEndpoints.cs`
- Modify: `be/src/MiniBanking.Api/Program.cs`

**Interfaces:**
- Produces: `MapPaymentEndpoints`, `MapAccountEndpoints`, `MapLedgerEndpoints` extension methods on `IEndpointRouteBuilder`.

- [ ] **Step 1: Create `PaymentEndpoints.cs` mapping `/api/v1/merchant/payments`, `/refunds`, `/settlements`**
- [ ] **Step 2: Create `AccountEndpoints.cs` mapping `/api/v1/accounts`**
- [ ] **Step 3: Create `LedgerEndpoints.cs` mapping `/api/v1/ledger`**
- [ ] **Step 4: Clean up `Program.cs` (remove all inline payment/refund maps, call extension methods only)**
- [ ] **Step 5: Run build and verify 0 warnings**
  Run: `dotnet build be/MiniBanking.sln`
- [ ] **Step 6: Commit changes**
  Run: `git commit -am "refactor(routing): extract module endpoints and clean up Program.cs"`

---

### Task 5: Verification & End-to-End Tests

**Files:**
- Create: `be/tests/MiniBanking.Tests/PaymentFlowIntegrationTests.cs`
- Create: `be/tests/MiniBanking.Tests/LedgerInvariantTests.cs`

- [ ] **Step 1: Write test for Double-Entry Balanced Invariant**
- [ ] **Step 2: Write test for Idempotency Replay with same key & conflict on different body**
- [ ] **Step 3: Run full test suite**
  Run: `dotnet test be/MiniBanking.sln`
- [ ] **Step 4: Commit changes and push to git**
  Run: `git commit -am "test: add integration and invariant verification tests"`
