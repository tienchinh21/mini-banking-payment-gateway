using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;
using System.Text.Json;

namespace MiniBanking.Modules.Payments.Application;

public record CreatePaymentRequest(
    string MerchantOrderId,
    string WalletAccountId,
    long Amount,
    string Currency,
    string Description,
    string? CallbackUrl);

public record PaymentResponse(
    Guid PaymentId,
    string MerchantOrderId,
    string Status,
    long Amount,
    string Currency,
    string? FailureCode = null);

public class CreatePaymentCommand : IRequest<PaymentResponse>
{
    public string MerchantId { get; }
    public string IdempotencyKey { get; }
    public string RequestMethod { get; }
    public string RequestPath { get; }
    public string RequestBody { get; }
    public CreatePaymentRequest Request { get; }

    public CreatePaymentCommand(
        string merchantId,
        string idempotencyKey,
        string requestMethod,
        string requestPath,
        string requestBody,
        CreatePaymentRequest request)
    {
        MerchantId = merchantId;
        IdempotencyKey = idempotencyKey;
        RequestMethod = requestMethod;
        RequestPath = requestPath;
        RequestBody = requestBody;
        Request = request;
    }
}

public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, PaymentResponse>
{
    private readonly MiniBankingDbContext _context;

    public CreatePaymentHandler(MiniBankingDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentResponse> Handle(CreatePaymentCommand command, CancellationToken cancellationToken)
    {
        var bodyHash = HmacSignatureService.ComputeBodyHash(command.RequestBody);

        // ── Idempotency check ────────────────────────────────────────────────────
        // Read the existing record *before* opening the write transaction so we
        // can return early without acquiring any row locks.
        var existingRecord = await _context.IdempotencyRecords
            .FirstOrDefaultAsync(
                r => r.MerchantId == command.MerchantId && r.Key == command.IdempotencyKey,
                cancellationToken);

        if (existingRecord is not null)
        {
            // Same key but different body → conflict; reject immediately.
            if (existingRecord.RequestBodyHash != bodyHash)
                throw new InvalidOperationException(
                    "Idempotency key was used with a different request body.");

            // Already completed → replay the stored response without touching
            // any balance or ledger data.
            if (existingRecord.Status == "Completed" && !string.IsNullOrEmpty(existingRecord.ResponsePayload))
                return JsonSerializer.Deserialize<PaymentResponse>(existingRecord.ResponsePayload)!;
        }

        // ── Validate input early (before acquiring locks) ────────────────────────
        if (!Guid.TryParse(command.Request.WalletAccountId, out var walletAccountId))
        {
            // We still need a DB round-trip to persist the idempotency record.
            var failedResponse = new PaymentResponse(
                Guid.Empty,
                command.Request.MerchantOrderId,
                "Failed",
                command.Request.Amount,
                command.Request.Currency,
                "INVALID_WALLET_ACCOUNT_ID");

            await PersistIdempotencyRecord(existingRecord, command, bodyHash, failedResponse, cancellationToken);
            return failedResponse;
        }

        // Validate the Money value object before touching the database.
        Money amount;
        try
        {
            amount = new Money(command.Request.Amount, command.Request.Currency);
            if (!amount.IsPositive)
                throw new ArgumentException("Amount must be positive.");
        }
        catch (Exception)
        {
            var failedResponse = new PaymentResponse(
                Guid.Empty,
                command.Request.MerchantOrderId,
                "Failed",
                command.Request.Amount,
                command.Request.Currency,
                "INVALID_AMOUNT");

            await PersistIdempotencyRecord(existingRecord, command, bodyHash, failedResponse, cancellationToken);
            return failedResponse;
        }

        // ── Serialisable transaction with row-level lock ──────────────────────────
        // The SELECT … FOR UPDATE issued below ensures only one concurrent
        // transaction holds the balance row lock at a time.  Any competing
        // transaction will block at the SELECT until this one commits or rolls
        // back, making the balance check + debit an atomic critical section at
        // the database level.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Lock the balance snapshot row for the duration of this transaction.
            // No other concurrent payment can read or modify this row until we commit.
            var balance = await _context.BalanceSnapshots
                .FromSqlInterpolated(
                    $"SELECT * FROM public.balance_snapshots WHERE \"WalletAccountId\" = {walletAccountId} FOR UPDATE")
                .FirstOrDefaultAsync(cancellationToken);

            if (balance is null)
            {
                var failedResponse = new PaymentResponse(
                    Guid.Empty,
                    command.Request.MerchantOrderId,
                    "Failed",
                    command.Request.Amount,
                    command.Request.Currency,
                    "WALLET_NOT_FOUND");

                await CompleteIdempotencyAndSave(existingRecord, command, bodyHash, failedResponse, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return failedResponse;
            }

            // ── Check currency compatibility before calling Debit ─────────────────
            if (balance.Currency != amount.Currency)
            {
                var failedResponse = new PaymentResponse(
                    Guid.Empty,
                    command.Request.MerchantOrderId,
                    "Failed",
                    command.Request.Amount,
                    command.Request.Currency,
                    "CURRENCY_MISMATCH");

                await CompleteIdempotencyAndSave(existingRecord, command, bodyHash, failedResponse, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return failedResponse;
            }

            // ── Insufficient funds check (delegate to domain method) ──────────────
            // Using balance.Debit() directly means the domain's guard is the single
            // source of truth; a raw balance check here would be redundant and could
            // diverge from the domain logic over time.
            if (balance.Available.Amount < amount.Amount)
            {
                var payment = new Payment(
                    command.MerchantId,
                    command.Request.MerchantOrderId,
                    walletAccountId,
                    amount,
                    command.Request.Description,
                    command.Request.CallbackUrl,
                    command.IdempotencyKey);

                payment.MarkFailed("INSUFFICIENT_FUNDS", "Wallet balance is not enough for this payment.");
                _context.Payments.Add(payment);

                var failedResponse = new PaymentResponse(
                    payment.Id,
                    command.Request.MerchantOrderId,
                    "Failed",
                    command.Request.Amount,
                    command.Request.Currency,
                    "INSUFFICIENT_FUNDS");

                await CompleteIdempotencyAndSave(existingRecord, command, bodyHash, failedResponse, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return failedResponse;
            }

            // ── Build and validate the double-entry ledger transaction ────────────
            var ledgerTransaction = new LedgerTransaction(
                $"PAY-{Guid.NewGuid():N}",
                LedgerTransactionType.Payment,
                command.Request.Description);

            ledgerTransaction.AddEntry(walletAccountId, "WalletAccount", amount, isDebit: true);
            ledgerTransaction.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", amount, isDebit: false);

            // Will throw if debits ≠ credits, preventing any unbalanced commit.
            ledgerTransaction.ValidateInvariant();

            _context.LedgerTransactions.Add(ledgerTransaction);

            // ── Debit the wallet (domain guard prevents negative balance) ─────────
            // At this point we hold the FOR UPDATE row lock so this debit is safe
            // from concurrent interference at the DB level.
            balance.Debit(amount);

            // ── Create the payment record ─────────────────────────────────────────
            var succeededPayment = new Payment(
                command.MerchantId,
                command.Request.MerchantOrderId,
                walletAccountId,
                amount,
                command.Request.Description,
                command.Request.CallbackUrl,
                command.IdempotencyKey);

            succeededPayment.MarkSucceeded(ledgerTransaction.Id);
            _context.Payments.Add(succeededPayment);

            var response = new PaymentResponse(
                succeededPayment.Id,
                succeededPayment.MerchantOrderId,
                "Succeeded",
                succeededPayment.Amount,
                succeededPayment.Currency);

            await CompleteIdempotencyAndSave(existingRecord, command, bodyHash, response, cancellationToken);

            // ── Publish outbox event ──────────────────────────────────────────────
            var outboxMessage = new OutboxMessage(
                "PaymentSucceeded",
                JsonSerializer.Serialize(new
                {
                    PaymentId      = succeededPayment.Id,
                    MerchantId     = succeededPayment.MerchantId,
                    MerchantOrderId = succeededPayment.MerchantOrderId,
                    Amount         = succeededPayment.Amount,
                    Currency       = succeededPayment.Currency,
                    WalletAccountId = succeededPayment.WalletAccountId,
                    Timestamp      = DateTime.UtcNow
                }));

            _context.OutboxMessages.Add(outboxMessage);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Persists an idempotency record outside of the main payment transaction
    /// (used for early-exit failure paths before the row lock is acquired).
    /// </summary>
    private async Task PersistIdempotencyRecord(
        IdempotencyRecord? existingRecord,
        CreatePaymentCommand command,
        string bodyHash,
        PaymentResponse response,
        CancellationToken cancellationToken)
    {
        var record = existingRecord ?? new IdempotencyRecord(
            command.MerchantId,
            command.IdempotencyKey,
            command.RequestMethod,
            command.RequestPath,
            bodyHash);

        if (existingRecord is null)
            _context.IdempotencyRecords.Add(record);

        record.Complete(JsonSerializer.Serialize(response));
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Attaches / updates the idempotency record inside the active transaction.
    /// Does NOT call SaveChangesAsync – the caller does so after adding other
    /// entities to the same unit-of-work.
    /// </summary>
    private async Task CompleteIdempotencyAndSave(
        IdempotencyRecord? existingRecord,
        CreatePaymentCommand command,
        string bodyHash,
        PaymentResponse response,
        CancellationToken cancellationToken)
    {
        var record = existingRecord ?? new IdempotencyRecord(
            command.MerchantId,
            command.IdempotencyKey,
            command.RequestMethod,
            command.RequestPath,
            bodyHash);

        if (existingRecord is null)
            _context.IdempotencyRecords.Add(record);

        record.Complete(JsonSerializer.Serialize(response));
        await _context.SaveChangesAsync(cancellationToken);
    }
}
