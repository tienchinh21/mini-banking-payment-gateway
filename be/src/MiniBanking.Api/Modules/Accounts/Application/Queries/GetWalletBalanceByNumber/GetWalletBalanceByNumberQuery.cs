using MediatR;

namespace MiniBanking.Modules.Accounts.Application.Queries.GetWalletBalanceByNumber;

public sealed record WalletBalanceByNumberResponse(
    string AccountNumber,
    string Currency,
    long AvailableBalance,
    long LedgerBalance);

public sealed record GetWalletBalanceByNumberQuery(string AccountNumber) : IRequest<WalletBalanceByNumberResponse?>;
