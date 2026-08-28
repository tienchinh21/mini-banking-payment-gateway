using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;
using MiniBanking.SharedKernel.Contracts;
using System.Text.Json;

namespace MiniBanking.Modules.Payments.Application.CreateRefund;

public sealed class CreateRefundHandler : IRequestHandler<CreateRefundCommand, CreateRefundResponse>
{
    private readonly MiniBankingDbContext _dbContext;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IAccountLockService _accountLockService;
    private readonly ILedgerPostingService _ledgerPostingService;

    public CreateRefundHandler(
        MiniBankingDbContext dbContext,
        IIdempotencyService idempotencyService,
        IAccountLockService accountLockService,
        ILedgerPostingService ledgerPostingService)
    {
        _dbContext = dbContext;
        _idempotencyService = idempotencyService;
        _accountLockService = accountLockService;
        _ledgerPostingService = ledgerPostingService;
    }

    public async Task<CreateRefundResponse> Handle(CreateRefundCommand command, CancellationToken cancellationToken)
    {
        var bodyHash = HmacSignatureService.ComputeBodyHash(command.RequestBody);

        // 1. Idempotency Check
        var (isCompleted, cachedResponse, idempotencyRecord) = await _idempotencyService.CheckOrInitializeAsync<CreateRefundResponse>(
            command.MerchantId, command.IdempotencyKey, command.RequestMethod, command.RequestPath, bodyHash, cancellationToken);

        if (isCompleted && cachedResponse is not null)
            return cachedResponse;

        // 2. Validate Payment
        var payment = await _dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == command.Request.PaymentId, cancellationToken);

        if (payment is null || payment.Status != PaymentStatus.Succeeded)
        {
            var fail = new CreateRefundResponse(Guid.Empty, command.Request.MerchantRefundId, command.Request.PaymentId, "Failed", command.Request.Amount, command.Request.Currency, "PAYMENT_NOT_FOUND_OR_NOT_SUCCEEDED");
            _idempotencyService.Complete(idempotencyRecord, fail);
            return fail;
        }

        if (payment.MerchantId != command.MerchantId)
        {
            var fail = new CreateRefundResponse(Guid.Empty, command.Request.MerchantRefundId, command.Request.PaymentId, "Failed", command.Request.Amount, command.Request.Currency, "PAYMENT_MERCHANT_MISMATCH");
            _idempotencyService.Complete(idempotencyRecord, fail);
            return fail;
        }

        var amount = new Money(command.Request.Amount, command.Request.Currency);
        if (amount.Currency != payment.Currency)
        {
            var fail = new CreateRefundResponse(Guid.Empty, command.Request.MerchantRefundId, command.Request.PaymentId, "Failed", command.Request.Amount, command.Request.Currency, "CURRENCY_MISMATCH");
            _idempotencyService.Complete(idempotencyRecord, fail);
            return fail;
        }

        var refundedAmount = await _dbContext.Refunds
            .AsNoTracking()
            .Where(r => r.PaymentId == payment.Id && r.Status == RefundStatus.Succeeded)
            .SumAsync(r => r.Amount, cancellationToken);

        if (refundedAmount + amount.Amount > payment.Amount)
        {
            var fail = new CreateRefundResponse(Guid.Empty, command.Request.MerchantRefundId, command.Request.PaymentId, "Failed", command.Request.Amount, command.Request.Currency, "REFUND_AMOUNT_EXCEEDED");
            _idempotencyService.Complete(idempotencyRecord, fail);
            return fail;
        }

        // 3. Lock Wallet Account & Credit with Row Lock
        var lockResult = await _accountLockService.LockAndCreditWalletAsync(payment.WalletAccountId, amount, cancellationToken);
        if (!lockResult.IsSuccess)
        {
            var fail = new CreateRefundResponse(Guid.Empty, command.Request.MerchantRefundId, command.Request.PaymentId, "Failed", command.Request.Amount, command.Request.Currency, lockResult.ErrorCode);
            _idempotencyService.Complete(idempotencyRecord, fail);
            return fail;
        }

        // 4. Post Balanced Double-Entry Refund Ledger Transaction
        var ledgerTx = await _ledgerPostingService.PostRefundAsync(payment.WalletAccountId, amount, command.Request.Reason, cancellationToken);

        // 5. Create Refund Entity
        var refund = new Refund(
            command.MerchantId, command.Request.MerchantRefundId, payment.Id, amount, command.Request.Reason, command.IdempotencyKey);
        refund.MarkSucceeded(ledgerTx.Id);
        _dbContext.Refunds.Add(refund);

        var response = new CreateRefundResponse(
            refund.Id, refund.MerchantRefundId, refund.PaymentId, "Succeeded", refund.Amount, refund.Currency, null);

        _idempotencyService.Complete(idempotencyRecord, response);

        // 6. Record Outbox Event
        var outboxMessage = new OutboxMessage(
            "RefundSucceeded",
            JsonSerializer.Serialize(new
            {
                RefundId = refund.Id,
                PaymentId = refund.PaymentId,
                MerchantId = refund.MerchantId,
                Amount = refund.Amount,
                Currency = refund.Currency,
                Timestamp = DateTime.UtcNow
            }));
        _dbContext.OutboxMessages.Add(outboxMessage);

        return response;
    }
}
