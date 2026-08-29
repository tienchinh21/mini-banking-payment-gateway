using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;

namespace MiniBanking.Modules.Payments.Application.Queries.GetPaymentById;

public sealed class GetPaymentByIdHandler : IRequestHandler<GetPaymentByIdQuery, PaymentDetailResponse?>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetPaymentByIdHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentDetailResponse?> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await _dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.PaymentId, cancellationToken);

        if (payment is null)
            return null;

        var wallet = await _dbContext.WalletAccounts
            .Include(w => w.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == payment.WalletAccountId, cancellationToken);

        var refunds = await _dbContext.Refunds
            .AsNoTracking()
            .Where(r => r.PaymentId == payment.Id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var refundDtos = refunds.Select(r => new PaymentRefundDto(
            r.Id,
            r.MerchantRefundId,
            r.Amount,
            r.Currency,
            r.Status.ToString(),
            r.Reason,
            r.CreatedAt
        )).ToList();

        return new PaymentDetailResponse(
            payment.Id,
            payment.MerchantId,
            payment.MerchantOrderId,
            payment.WalletAccountId,
            wallet?.AccountNumber,
            wallet?.Customer.FullName,
            wallet?.Customer.Email,
            payment.Amount,
            payment.Currency,
            payment.Status.ToString(),
            payment.FailureCode,
            payment.Description,
            payment.IdempotencyKey,
            payment.LedgerTransactionId,
            payment.CreatedAt,
            payment.UpdatedAt,
            refundDtos);
    }
}
