using MediatR;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;
using MiniBanking.SharedKernel.Contracts;
using System.Text.Json;

namespace MiniBanking.Modules.Payments.Application.CreatePayment;

public sealed class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, PaymentResponse>
{
    private readonly MiniBankingDbContext _context;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IAccountLockService _accountLockService;
    private readonly ILedgerPostingService _ledgerPostingService;

    public CreatePaymentHandler(
        MiniBankingDbContext context,
        IIdempotencyService idempotencyService,
        IAccountLockService accountLockService,
        ILedgerPostingService ledgerPostingService)
    {
        _context = context;
        _idempotencyService = idempotencyService;
        _accountLockService = accountLockService;
        _ledgerPostingService = ledgerPostingService;
    }

    public async Task<PaymentResponse> Handle(CreatePaymentCommand command, CancellationToken cancellationToken)
    {
        var bodyHash = HmacSignatureService.ComputeBodyHash(command.RequestBody);

        // 1. Idempotency Check
        var (isCompleted, cachedResponse, idempotencyRecord) = await _idempotencyService.CheckOrInitializeAsync<PaymentResponse>(
            command.MerchantId, command.IdempotencyKey, command.RequestMethod, command.RequestPath, bodyHash, cancellationToken);

        if (isCompleted && cachedResponse is not null)
            return cachedResponse;

        // 2. Early Input Validation
        if (!Guid.TryParse(command.Request.WalletAccountId, out var walletAccountId))
        {
            var failedResponse = new PaymentResponse(Guid.Empty, command.Request.MerchantOrderId, "Failed", command.Request.Amount, command.Request.Currency, "INVALID_WALLET_ACCOUNT_ID");
            _idempotencyService.Complete(idempotencyRecord, failedResponse);
            return failedResponse;
        }

        Money amount;
        try
        {
            amount = new Money(command.Request.Amount, command.Request.Currency);
            if (!amount.IsPositive) throw new ArgumentException("Amount must be positive.");
        }
        catch
        {
            var failedResponse = new PaymentResponse(Guid.Empty, command.Request.MerchantOrderId, "Failed", command.Request.Amount, command.Request.Currency, "INVALID_AMOUNT");
            _idempotencyService.Complete(idempotencyRecord, failedResponse);
            return failedResponse;
        }

        // 3. Lock Wallet Account & Debit with Row Lock
        var lockResult = await _accountLockService.LockAndDebitWalletAsync(walletAccountId, amount, cancellationToken);
        if (!lockResult.IsSuccess)
        {
            var payment = new Payment(
                command.MerchantId, command.Request.MerchantOrderId, walletAccountId, amount,
                command.Request.Description, command.Request.CallbackUrl, command.IdempotencyKey);
            payment.MarkFailed(lockResult.ErrorCode ?? "PAYMENT_FAILED", lockResult.ErrorMessage);
            _context.Payments.Add(payment);

            var failedResponse = new PaymentResponse(payment.Id, command.Request.MerchantOrderId, "Failed", command.Request.Amount, command.Request.Currency, lockResult.ErrorCode);
            _idempotencyService.Complete(idempotencyRecord, failedResponse);
            return failedResponse;
        }

        // 4. Post Balanced Double-Entry Ledger Transaction
        var ledgerTx = await _ledgerPostingService.PostPaymentAsync(walletAccountId, amount, command.Request.Description, cancellationToken);

        // 5. Create Payment Entity
        var succeededPayment = new Payment(
            command.MerchantId, command.Request.MerchantOrderId, walletAccountId, amount,
            command.Request.Description, command.Request.CallbackUrl, command.IdempotencyKey);
        succeededPayment.MarkSucceeded(ledgerTx.Id);
        _context.Payments.Add(succeededPayment);

        var response = new PaymentResponse(
            succeededPayment.Id, succeededPayment.MerchantOrderId, "Succeeded", succeededPayment.Amount, succeededPayment.Currency);

        _idempotencyService.Complete(idempotencyRecord, response);

        // 6. Record Outbox Event
        var outboxMessage = new OutboxMessage(
            "PaymentSucceeded",
            JsonSerializer.Serialize(new
            {
                PaymentId = succeededPayment.Id,
                MerchantId = succeededPayment.MerchantId,
                MerchantOrderId = succeededPayment.MerchantOrderId,
                Amount = succeededPayment.Amount,
                Currency = succeededPayment.Currency,
                WalletAccountId = succeededPayment.WalletAccountId,
                Timestamp = DateTime.UtcNow
            }));
        _context.OutboxMessages.Add(outboxMessage);

        return response;
    }
}
