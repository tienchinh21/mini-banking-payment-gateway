using MediatR;

namespace MiniBanking.Modules.Merchants.Application.Queries.GetMerchants;

public sealed record GetMerchantsQuery(
    int? Page = 1,
    int? PageSize = 20,
    string? Search = null,
    bool? IsActive = null
) : IRequest<GetMerchantsResponse>;

public sealed record MerchantListItemDto(
    Guid Id,
    string MerchantId,
    string Code,
    string Name,
    string ApiKey,
    string SecretMasked,
    string? WebhookUrl,
    bool IsActive,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record PaginationMeta(
    int CurrentPage,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNext,
    bool HasPrevious
);

public sealed record GetMerchantsResponse(
    IReadOnlyList<MerchantListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    PaginationMeta Meta
);
