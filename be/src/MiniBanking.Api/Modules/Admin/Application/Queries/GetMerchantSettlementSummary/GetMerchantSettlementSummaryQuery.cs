using MediatR;

namespace MiniBanking.Modules.Admin.Application.Queries.GetMerchantSettlementSummary;

public sealed record GetMerchantSettlementSummaryQuery(string? MerchantId = null)
    : IRequest<MerchantSettlementSummaryDto>;

public sealed record MerchantSettlementSummaryDto(
    int TotalMerchants,
    long TotalGrossAmount,
    long TotalRefundAmount,
    long TotalSettledAmount,
    long TotalPendingSettlementAmount,
    IReadOnlyList<MerchantSettlementItemDto> Items);

public sealed record MerchantSettlementItemDto(
    string MerchantId,
    string MerchantName,
    int TotalPayments,
    long TotalPaymentAmount,
    int TotalRefunds,
    long TotalRefundAmount,
    int TotalSettlements,
    long TotalSettledAmount,
    long NetBalance,
    long PendingSettlementAmount,
    DateTime? LastSettlementDate);
