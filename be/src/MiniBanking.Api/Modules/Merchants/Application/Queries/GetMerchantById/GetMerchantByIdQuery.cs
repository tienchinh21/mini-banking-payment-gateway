using MediatR;

namespace MiniBanking.Modules.Merchants.Application.Queries.GetMerchantById;

public sealed record GetMerchantByIdQuery(string Id) : IRequest<MerchantDetailResponse?>;

public sealed record MerchantDetailResponse(
    Guid Id,
    string MerchantId,
    string Code,
    string Name,
    string ApiKey,
    string Secret,
    string? WebhookUrl,
    bool IsActive,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int TotalPayments,
    long TotalVolume,
    long TotalPaidAmount
);
