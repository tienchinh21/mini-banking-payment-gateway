using MediatR;

namespace MiniBanking.Modules.Accounts.Application.Queries.GetWalletDetail;

public sealed record RecentTransactionDto(
    Guid Id,
    Guid TransactionId,
    long Amount,
    string Currency,
    bool IsDebit,
    DateTime CreatedAt);

public sealed record WalletDetailResponse(
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
    DateTime? UpdatedAt,
    IReadOnlyList<RecentTransactionDto> RecentTransactions);

public sealed record GetWalletDetailQuery(string Identifier) : IRequest<WalletDetailResponse?>;
