using MediatR;

namespace MiniBanking.Modules.Admin.Application.Queries.GetDashboardStats;

public sealed record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public sealed record DashboardStatsDto(
    DashboardWalletsDto Wallets,
    DashboardMerchantsDto Merchants,
    DashboardPaymentsDto Payments,
    DashboardRefundsDto Refunds,
    IReadOnlyList<RecentPaymentDto> RecentPayments);

public sealed record DashboardWalletsDto(
    int Total,
    int Customers);

public sealed record DashboardMerchantsDto(
    int Total);

public sealed record DashboardPaymentsDto(
    int Total,
    int Successful,
    int Failed,
    long TotalVolume,
    long TodayVolume,
    int TodayCount);

public sealed record DashboardRefundsDto(
    int TotalCount,
    long TotalAmount);

public sealed record RecentPaymentDto(
    Guid Id,
    string MerchantId,
    string MerchantOrderId,
    long Amount,
    string Currency,
    string Status,
    DateTime CreatedAt);
