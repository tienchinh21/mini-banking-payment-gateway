---
name: dotnet-cqrs-clean-services
description: "Rules and design standards for MediatR CQRS handlers, Domain Services, and Pipeline Behaviors in .NET. Forbids 'God Handlers' and enforces single responsibility."
---

# .NET CQRS & Clean Services Standard

Defines how Command/Query Handlers, Domain Services, and Pipeline Behaviors must be structured to prevent bloated, unmaintainable handlers.

## 1. Single Responsibility for Handlers

- A **MediatR Handler** is an **Application Orchestrator**, not a place to dump 200+ lines of low-level infrastructure logic.
- Target handler length: **25 - 50 lines max**.
- **What a Handler DOES**:
  1. Receive Command / Query DTO.
  2. Coordinate Domain Entities & Domain Services.
  3. Emit Domain Events / Outbox Messages.
  4. Return a response DTO.
- **What a Handler DOES NOT do**:
  - Compute raw cryptographic hashes (delegate to `IHmacSignatureService`).
  - Parse HTTP request streams.
  - Manually manage transactions, logging, or metric recording (delegate to **MediatR Pipeline Behaviors**).
  - Contain raw SQL queries or complex table-level joining.

## 2. Layering & Responsibility Separation

```text
┌────────────────────────────────────────────────────────────────────────┐
│ 1. Minimal API Endpoint (Extracts HTTP headers, creates Command)       │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ mediator.Send(command)
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ 2. MediatR Pipeline Behaviors (Cross-Cutting Concerns)                 │
│    ├── ValidationBehavior<TRequest, TResponse> (FluentValidation)      │
│    ├── LoggingBehavior<TRequest, TResponse> (Serilog CorrelationId)    │
│    └── TransactionBehavior<TRequest, TResponse> (Auto DB Transaction)  │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ 3. Command Handler (Application Orchestrator, ~30-40 lines)            │
│    ├── Calls IdempotencyGuard                                          │
│    ├── Calls ILedgerPostingService (Domain Service)                    │
│    └── Appends OutboxMessage                                           │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ 4. Rich Domain Entities & Value Objects (Business Invariants)          │
│    ├── BalanceSnapshot.Debit(Money amount) -> Guards against negative  │
│    └── LedgerTransaction.ValidateInvariant() -> Guards Debits=Credits │
└────────────────────────────────────────────────────────────────────────┘
```

## 3. Example Clean Handler

```csharp
public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, PaymentResponse>
{
    private readonly IIdempotencyService _idempotencyService;
    private readonly IAccountLockService _accountLockService;
    private readonly ILedgerPostingService _ledgerPostingService;
    private readonly IOutboxService _outboxService;

    public CreatePaymentHandler(
        IIdempotencyService idempotencyService,
        IAccountLockService accountLockService,
        ILedgerPostingService ledgerPostingService,
        IOutboxService outboxService)
    {
        _idempotencyService = idempotencyService;
        _accountLockService = accountLockService;
        _ledgerPostingService = ledgerPostingService;
        _outboxService = outboxService;
    }

    public async Task<PaymentResponse> Handle(CreatePaymentCommand command, CancellationToken ct)
    {
        // 1. Idempotency Check
        var (isReplay, cachedResponse) = await _idempotencyService.CheckAsync(command.MerchantId, command.IdempotencyKey, command.PayloadHash, ct);
        if (isReplay) return cachedResponse!;

        // 2. Lock and Debit Wallet
        var debitResult = await _accountLockService.DebitWithRowLockAsync(command.WalletAccountId, command.Amount, ct);
        if (!debitResult.IsSuccess)
        {
            return await _idempotencyService.SaveFailedAsync(command, debitResult.ErrorCode, ct);
        }

        // 3. Post Balanced Double-Entry Ledger
        var ledgerTx = await _ledgerPostingService.PostPaymentAsync(command.WalletAccountId, command.Amount, ct);

        // 4. Save Outbox Event & Complete Idempotency
        await _outboxService.PublishAsync(new PaymentSucceededEvent(command.PaymentId, command.Amount), ct);
        return await _idempotencyService.SaveCompletedAsync(command, ledgerTx.Id, ct);
    }
}
```
