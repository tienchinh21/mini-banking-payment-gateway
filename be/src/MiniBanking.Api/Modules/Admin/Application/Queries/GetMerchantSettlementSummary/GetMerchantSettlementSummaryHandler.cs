using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Payments.Domain;

namespace MiniBanking.Modules.Admin.Application.Queries.GetMerchantSettlementSummary;

public sealed class GetMerchantSettlementSummaryHandler
    : IRequestHandler<GetMerchantSettlementSummaryQuery, MerchantSettlementSummaryDto>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetMerchantSettlementSummaryHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MerchantSettlementSummaryDto> Handle(
        GetMerchantSettlementSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var merchantsQuery = _dbContext.Merchants.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.MerchantId))
        {
            merchantsQuery = merchantsQuery.Where(m => m.MerchantId == request.MerchantId);
        }
        var merchants = await merchantsQuery.ToListAsync(cancellationToken);

        var paymentsQuery = _dbContext.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Succeeded);
        if (!string.IsNullOrWhiteSpace(request.MerchantId))
        {
            paymentsQuery = paymentsQuery.Where(p => p.MerchantId == request.MerchantId);
        }
        var paymentStats = await paymentsQuery
            .GroupBy(p => p.MerchantId)
            .Select(g => new
            {
                MerchantId = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(p => p.Amount)
            })
            .ToDictionaryAsync(x => x.MerchantId, cancellationToken);

        var refundsQuery = _dbContext.Refunds.AsNoTracking()
            .Where(r => r.Status == RefundStatus.Succeeded);
        if (!string.IsNullOrWhiteSpace(request.MerchantId))
        {
            refundsQuery = refundsQuery.Where(r => r.MerchantId == request.MerchantId);
        }
        var refundStats = await refundsQuery
            .GroupBy(r => r.MerchantId)
            .Select(g => new
            {
                MerchantId = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(r => r.Amount)
            })
            .ToDictionaryAsync(x => x.MerchantId, cancellationToken);

        var settlementsQuery = _dbContext.Settlements.AsNoTracking()
            .Where(s => s.Status == SettlementStatus.Completed);
        if (!string.IsNullOrWhiteSpace(request.MerchantId))
        {
            settlementsQuery = settlementsQuery.Where(s => s.MerchantId == request.MerchantId);
        }
        var settlementStats = await settlementsQuery
            .GroupBy(s => s.MerchantId)
            .Select(g => new
            {
                MerchantId = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(s => s.Amount),
                LastSettlementDate = g.Max(s => (DateTime?)s.CreatedAt)
            })
            .ToDictionaryAsync(x => x.MerchantId, cancellationToken);

        var items = merchants.Select(m =>
        {
            paymentStats.TryGetValue(m.MerchantId, out var p);
            refundStats.TryGetValue(m.MerchantId, out var r);
            settlementStats.TryGetValue(m.MerchantId, out var s);

            var payCount = p?.Count ?? 0;
            var payAmount = p?.TotalAmount ?? 0L;
            var refCount = r?.Count ?? 0;
            var refAmount = r?.TotalAmount ?? 0L;
            var setCount = s?.Count ?? 0;
            var setAmount = s?.TotalAmount ?? 0L;
            var pendingAmount = Math.Max(0L, payAmount - refAmount - setAmount);

            return new MerchantSettlementItemDto(
                m.MerchantId,
                m.Name,
                payCount,
                payAmount,
                refCount,
                refAmount,
                setCount,
                setAmount,
                pendingAmount,
                pendingAmount,
                s?.LastSettlementDate);
        }).ToList();

        var totalGross = items.Sum(i => i.TotalPaymentAmount);
        var totalRefund = items.Sum(i => i.TotalRefundAmount);
        var totalSettled = items.Sum(i => i.TotalSettledAmount);
        var totalPending = items.Sum(i => i.PendingSettlementAmount);

        return new MerchantSettlementSummaryDto(
            items.Count,
            totalGross,
            totalRefund,
            totalSettled,
            totalPending,
            items);
    }
}
