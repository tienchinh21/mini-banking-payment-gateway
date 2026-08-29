using MediatR;

namespace MiniBanking.Modules.Accounts.Application.Queries.GetWalletBalance;

public sealed record WalletBalanceResponse(
    Guid WalletAccountId,
    long AvailableBalance,
    long LedgerBalance,
    string Currency,
    long Version);

public sealed record GetWalletBalanceQuery(Guid WalletAccountId) : IRequest<WalletBalanceResponse?>;
