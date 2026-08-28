---
name: fintech-ledger-concurrency-engine
description: "Core banking rules for money handling, double-entry bookkeeping, row-level locking, and idempotency guarantees in .NET."
---

# Fintech Core: Money, Double-Entry Ledger, Concurrency & Idempotency

Defines the non-negotiable financial invariants and concurrency rules for banking and payment engines.

## 1. Golden Invariants of Money

- **Minor Unit Storage**: Never use floating-point types (`float`, `double`, `decimal`) for money storage. Always store minor units (e.g. `VND`, `Cents`) as `long` integers to eliminate rounding bugs.
- **Value Object Enforcement**: All amounts must be paired with their currency using the `Money` value object (`new Money(500000, "VND")`). Currency mismatches must throw domain exceptions immediately.
- **No Direct Balance Mutating**: `AvailableBalance` is an operational snapshot/cache. Every financial change must originate from an immutable **Ledger Transaction**.

## 2. Double-Entry Bookkeeping Standard

- **Append-Only Ledger**: The `ledger_transactions` and `ledger_entries` tables are **strictly append-only**. `UPDATE` and `DELETE` queries are prohibited at the application and database level.
- **Balanced Transaction Invariant**:
  $$\sum \text{Debit Amounts} = \sum \text{Credit Amounts} \quad (\text{for the same currency})$$
  If the sum of Debits does not equal the sum of Credits, the domain method `ValidateInvariant()` throws an exception and rolls back the database transaction.

```csharp
// Example Double-Entry Entry Setup
var tx = new LedgerTransaction($"PAY-{Guid.NewGuid():N}", LedgerTransactionType.Payment, "Direct Debit");
tx.AddEntry(customerWalletId, "WalletAccount", amount, isDebit: true);     // Debit Customer (-500k)
tx.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", amount, isDebit: false); // Credit Clearing (+500k)
tx.ValidateInvariant(); // Throws if not perfectly balanced
```

## 3. Concurrency Control & Race Condition Prevention

- **Pessimistic Row Lock (`SELECT FOR UPDATE`)**:
  When debiting a wallet, the row in `balance_snapshots` must be locked at the PostgreSQL level within an active transaction:
  ```sql
  SELECT * FROM public.balance_snapshots WHERE "WalletAccountId" = @walletId FOR UPDATE;
  ```
- **Optimistic Version Token**:
  The `Version` column must increment monotonically with every credit/debit. If an unexpected concurrency collision occurs, EF Core will trigger a `DbUpdateConcurrencyException`.

## 4. Idempotency Key Specification

- **3-Phase Lifecycle**:
  `Started` $\rightarrow$ `Completed` / `Failed`.
- **Payload Hash Protection**:
  Compute `SHA256(RequestBody)`. If a request reuses an existing `Idempotency-Key` but provides a different payload hash, reject immediately with `409 Conflict` (Fraud / Request tampering detected).
- **Fast Path Return**:
  If status is `Completed`, return the cached `ResponsePayload` immediately without entering the database lock or debiting funds again.
