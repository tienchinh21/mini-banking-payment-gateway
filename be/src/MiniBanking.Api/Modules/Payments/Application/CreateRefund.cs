using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;
using System.Text.Json;

namespace MiniBanking.Modules.Payments.Application;

public sealed record CreateRefundRequest(
    string MerchantRefundId,
    Guid PaymentId,
    long Amount,
    string Currency,
    string Reason);

public sealed record CreateRefundResponse(
    Guid RefundId,
    string MerchantRefundId,
    Guid PaymentId,
    string Status,
    long Amount,
    string Currency,
    string? FailureCode);

public sealed class CreateRefundCommand : IRequest<CreateRefundResponse>
{
    public string MerchantId { get; }
    public string IdempotencyKey { get; }
    public string RequestMethod { get; }
    public string RequestPath { get; }
    public string RequestBody { get; }
    public CreateRefundRequest Request { get; }

    public CreateRefundCommand(
        string merchantId,
        string idempotencyKey,
        string requestMethod,
        string requestPath,
        string requestBody,
        CreateRefundRequest request)
    {
        MerchantId = merchantId;
        IdempotencyKey = idempotencyKey;
        RequestMethod = requestMethod;
        RequestPath = requestPath;
        RequestBody = requestBody;
        Request = request;
    }
}

public sealed class CreateRefundCommandHandler : IRequestHandler<CreateRefundCommand, CreateRefundResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public CreateRefundCommandHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateRefundResponse> Handle(CreateRefundCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var bodyHash = HmacSignatureService.ComputeBodyHash(command.RequestBody);
            var existing = await _dbContext.IdempotencyRecords
                .FirstOrDefaultAsync(
                    r => r.MerchantId == command.MerchantId && r.Key == command.IdempotencyKey,
                    cancellationToken);

            if (existing is not null)
            {
                if (existing.RequestBodyHash != bodyHash)
                    throw new InvalidOperationException("Idempotency key mismatch.");

                if (existing.Status == "Succeeded")
                    return Deserialize(existing.ResponsePayload);

                _dbContext.IdempotencyRecords.Remove(existing);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var payment = await _dbContext.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == command.Request.PaymentId, cancellationToken);

            if (payment is null || payment.Status != PaymentStatus.Succeeded)
                return await FailAndRecordAsync(command, bodyHash, "PAYMENT_NOT_FOUND_OR_NOT_SUCCEEDED", cancellationToken);

            if (payment.MerchantId != command.MerchantId)
                return await FailAndRecordAsync(command, bodyHash, "PAYMENT_MERCHANT_MISMATCH", cancellationToken);

            var amount = new Money(command.Request.Amount, command.Request.Currency);
            if (amount.Currency != payment.Currency)
                return await FailAndRecordAsync(command, bodyHash, "CURRENCY_MISMATCH", cancellationToken);

            var refundedAmount = await _dbContext.Refunds
                .AsNoTracking()
                .Where(r => r.PaymentId == payment.Id && r.Status == RefundStatus.Succeeded)
                .SumAsync(r => r.Amount, cancellationToken);

            if (refundedAmount + amount.Amount > payment.Amount)
                return await FailAndRecordAsync(command, bodyHash, "REFUND_AMOUNT_EXCEEDED", cancellationToken);

            var wallet = await _dbContext.WalletAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == payment.WalletAccountId, cancellationToken);

            if (wallet is null)
                return await FailAndRecordAsync(command, bodyHash, "WALLET_NOT_FOUND", cancellationToken);

            var walletBalance = await _dbContext.BalanceSnapshots
                .FromSqlInterpolated($"SELECT * FROM balance_snapshots WHERE \"WalletAccountId\" = {wallet.Id} FOR UPDATE")
                .FirstOrDefaultAsync(cancellationToken);

            if (walletBalance is null)
                return await FailAndRecordAsync(command, bodyHash, "BALANCE_NOT_FOUND", cancellationToken);

            var refund = new Refund(
                command.MerchantId,
                command.Request.MerchantRefundId,
                payment.Id,
                amount,
                command.Request.Reason,
                command.IdempotencyKey);

            _dbContext.Refunds.Add(refund);

            var ledgerTransaction = new LedgerTransaction(
                $"REFUND-{refund.Id}",
                LedgerTransactionType.Refund,
                $"Refund for payment {payment.MerchantOrderId}");

            ledgerTransaction.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", amount, isDebit: true);
            ledgerTransaction.AddEntry(wallet.Id, "WalletAccount", amount, isDebit: false);
            ledgerTransaction.ValidateInvariant();

            _dbContext.LedgerTransactions.Add(ledgerTransaction);

            walletBalance.Credit(amount);
            _dbContext.BalanceSnapshots.Update(walletBalance);

            refund.MarkSucceeded(ledgerTransaction.Id);

            var response = new CreateRefundResponse(
                refund.Id,
                refund.MerchantRefundId,
                refund.PaymentId,
                "Succeeded",
                refund.Amount,
                refund.Currency,
                null);

            var outboxMessage = new OutboxMessage(
                "RefundSucceeded",
                JsonSerializer.Serialize(new
                {
                    RefundId = refund.Id,
                    PaymentId = refund.PaymentId,
                    MerchantId = refund.MerchantId,
                    MerchantRefundId = refund.MerchantRefundId,
                    Amount = refund.Amount,
                    Currency = refund.Currency,
                    Timestamp = DateTime.UtcNow
                }));

            _dbContext.OutboxMessages.Add(outboxMessage);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await RecordIdempotencyAsync(command, bodyHash, response, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return response;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static CreateRefundResponse Deserialize(string? payload)
    {
        if (string.IsNullOrEmpty(payload))
            throw new InvalidOperationException("Stored idempotency response is empty.");

        var stored = JsonSerializer.Deserialize<CreateRefundResponse>(payload);
        return stored ?? throw new InvalidOperationException("Stored idempotency response is invalid.");
    }

    private async Task<CreateRefundResponse> FailAndRecordAsync(
        CreateRefundCommand command,
        string bodyHash,
        string failureCode,
        CancellationToken cancellationToken)
    {
        var refund = new Refund(
            command.MerchantId,
            command.Request.MerchantRefundId,
            command.Request.PaymentId,
            new Money(command.Request.Amount, command.Request.Currency),
            command.Request.Reason,
            command.IdempotencyKey);

        refund.MarkFailed(failureCode);
        _dbContext.Refunds.Add(refund);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new CreateRefundResponse(
            refund.Id,
            refund.MerchantRefundId,
            refund.PaymentId,
            "Failed",
            refund.Amount,
            refund.Currency,
            failureCode);

        await RecordIdempotencyAsync(command, bodyHash, response, cancellationToken);
        return response;
    }

    private async Task RecordIdempotencyAsync(
        CreateRefundCommand command,
        string bodyHash,
        CreateRefundResponse response,
        CancellationToken cancellationToken)
    {
        var record = new IdempotencyRecord(
            command.MerchantId,
            command.IdempotencyKey,
            command.RequestMethod,
            command.RequestPath,
            bodyHash);

        record.Complete(JsonSerializer.Serialize(response));

        _dbContext.IdempotencyRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
