using MediatR;

namespace MiniBanking.Modules.Accounts.Application.Queries.GetWallets;

public sealed record WalletSummaryDto(
    Guid WalletId,
    string AccountNumber,
    string Currency,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    long AvailableBalance,
    long LedgerBalance,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record WalletsListResponse(
    IReadOnlyList<WalletSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record GetWalletsQuery(
    int? Page = 1,
    int? PageSize = 20,
    string? Search = null,
    string? Currency = null) : IRequest<WalletsListResponse>;
