using MediatR;

namespace MiniBanking.Modules.Accounts.Application.Queries.GetWalletLedger;

public sealed record WalletLedgerEntryDto(
    Guid Id,
    Guid TransactionId,
    long Amount,
    string Currency,
    bool IsDebit,
    DateTime CreatedAt);

public sealed record GetWalletLedgerQuery(string AccountNumber) : IRequest<IReadOnlyList<WalletLedgerEntryDto>?>;
