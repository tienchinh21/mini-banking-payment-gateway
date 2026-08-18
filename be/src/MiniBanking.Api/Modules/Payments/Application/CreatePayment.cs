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

        // Check existing idempotency record
        var existingRecord = await _context.IdempotencyRecords
            .FirstOrDefaultAsync(
                r => r.MerchantId == command.MerchantId && r.Key == command.IdempotencyKey,
                cancellationToken);

        if (existingRecord is not null)
        {
            if (existingRecord.RequestBodyHash != bodyHash)
                throw new InvalidOperationException("Idempotency key was used with a different request body.");

            if (existingRecord.Status == "Completed" && !string.IsNullOrEmpty(existingRecord.ResponsePayload))
                return JsonSerializer.Deserialize<PaymentResponse>(existingRecord.ResponsePayload)!;
        }

        var idempotencyRecord = existingRecord ?? new IdempotencyRecord(
            command.MerchantId,
            command.IdempotencyKey,
            command.RequestMethod,
            command.RequestPath,
            bodyHash);

        if (existingRecord is null)
            _context.IdempotencyRecords.Add(idempotencyRecord);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!Guid.TryParse(command.Request.WalletAccountId, out var walletAccountId))
            {
                var failedResponse = new PaymentResponse(Guid.Empty, command.Request.MerchantOrderId, "Failed", command.Request.Amount, command.Request.Currency, "INVALID_WALLET_ACCOUNT_ID");
                idempotencyRecord.Complete(JsonSerializer.Serialize(failedResponse));
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return failedResponse;
            }

            // Lock wallet balance snapshot row
            var balance = await _context.BalanceSnapshots
                .FromSqlInterpolated($"SELECT * FROM public.balance_snapshots WHERE \"WalletAccountId\" = {walletAccountId} FOR UPDATE")
                .FirstOrDefaultAsync(cancellationToken);

            if (balance is null)
            {
                var failedResponse = new PaymentResponse(Guid.Empty, command.Request.MerchantOrderId, "Failed", command.Request.Amount, command.Request.Currency, "WALLET_NOT_FOUND");
                idempotencyRecord.Complete(JsonSerializer.Serialize(failedResponse));
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return failedResponse;
            }

            var amount = new Money(command.Request.Amount, command.Request.Currency);

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

                var failedResponse = new PaymentResponse(payment.Id, command.Request.MerchantOrderId, "Failed", command.Request.Amount, command.Request.Currency, "INSUFFICIENT_FUNDS");
                idempotencyRecord.Complete(JsonSerializer.Serialize(failedResponse));

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return failedResponse;
            }

            // Create ledger transaction
            var ledgerTransaction = new LedgerTransaction(
                $"PAY-{Guid.NewGuid():N}",
                LedgerTransactionType.Payment,
                command.Request.Description);

            ledgerTransaction.AddEntry(walletAccountId, "WalletAccount", amount, isDebit: true);
            ledgerTransaction.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", amount, isDebit: false);
            ledgerTransaction.ValidateInvariant();

            _context.LedgerTransactions.Add(ledgerTransaction);

            // Update balance
            balance.Debit(amount);

            // Create payment record
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

            idempotencyRecord.Complete(JsonSerializer.Serialize(response));

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
}
