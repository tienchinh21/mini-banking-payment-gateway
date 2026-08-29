using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Payments.Domain;

namespace MiniBanking.Modules.Admin.Application.Queries.GetDashboardStats;

public sealed class GetDashboardStatsHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetDashboardStatsHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var totalWallets = await _dbContext.WalletAccounts.CountAsync(cancellationToken);
        var totalCustomers = await _dbContext.BankingCustomers.CountAsync(cancellationToken);
        var totalMerchants = await _dbContext.Merchants.CountAsync(m => m.IsActive, cancellationToken);

        var totalPayments = await _dbContext.Payments.CountAsync(cancellationToken);
        var successfulPayments = await _dbContext.Payments.CountAsync(p => p.Status == PaymentStatus.Succeeded, cancellationToken);
        var failedPayments = await _dbContext.Payments.CountAsync(p => p.Status == PaymentStatus.Failed, cancellationToken);

        var totalVolume = await _dbContext.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (long?)p.Amount, cancellationToken) ?? 0L;

        var totalRefunds = await _dbContext.Refunds.CountAsync(r => r.Status == RefundStatus.Succeeded, cancellationToken);
        var totalRefundAmount = await _dbContext.Refunds
            .Where(r => r.Status == RefundStatus.Succeeded)
            .SumAsync(r => (long?)r.Amount, cancellationToken) ?? 0L;

        var today = DateTime.UtcNow.Date;
        var todayPayments = await _dbContext.Payments
            .Where(p => p.CreatedAt >= today && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (long?)p.Amount, cancellationToken) ?? 0L;
        var todayTxCount = await _dbContext.Payments
            .CountAsync(p => p.CreatedAt >= today, cancellationToken);

        var recentPayments = await _dbContext.Payments
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentPaymentDto(
                p.Id,
                p.MerchantId,
                p.MerchantOrderId,
                p.Amount,
                p.Currency,
                p.Status.ToString(),
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new DashboardStatsDto(
            new DashboardWalletsDto(totalWallets, totalCustomers),
            new DashboardMerchantsDto(totalMerchants),
            new DashboardPaymentsDto(
                totalPayments,
                successfulPayments,
                failedPayments,
                totalVolume,
                todayPayments,
                todayTxCount),
            new DashboardRefundsDto(totalRefunds, totalRefundAmount),
            recentPayments);
    }
}
