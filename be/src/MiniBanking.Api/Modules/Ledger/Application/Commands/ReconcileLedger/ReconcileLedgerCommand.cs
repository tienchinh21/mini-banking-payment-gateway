using MediatR;

namespace MiniBanking.Modules.Ledger.Application.Commands.ReconcileLedger;

public sealed record WalletReconcileDetailDto(
    Guid WalletId,
    string AccountNumber,
    long SnapshotBalance,
    long CalculatedBalance,
    long TotalDebits,
    long TotalCredits,
    bool IsMatched,
    long Difference);

public sealed record ReconcileLedgerResponse(
    int TotalWallets,
    int DiscrepanciesCount,
    bool IsFullyReconciled,
    DateTime ReconciledAt,
    IReadOnlyList<WalletReconcileDetailDto> Details);

public sealed record ReconcileLedgerCommand : IRequest<ReconcileLedgerResponse>;
