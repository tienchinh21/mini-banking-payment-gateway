using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;

namespace MiniBanking.Modules.Ledger.Application.Commands.ReconcileLedger;

public sealed class ReconcileLedgerHandler : IRequestHandler<ReconcileLedgerCommand, ReconcileLedgerResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public ReconcileLedgerHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReconcileLedgerResponse> Handle(
        ReconcileLedgerCommand command,
        CancellationToken cancellationToken)
    {
        var wallets = await _dbContext.WalletAccounts
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var walletIds = wallets.Select(w => w.Id).ToList();

        var snapshots = await _dbContext.BalanceSnapshots
            .AsNoTracking()
            .Where(b => walletIds.Contains(b.WalletAccountId))
            .ToDictionaryAsync(b => b.WalletAccountId, cancellationToken);

        var entries = await _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => walletIds.Contains(e.AccountId))
            .ToListAsync(cancellationToken);

        var entriesByWallet = entries
            .GroupBy(e => e.AccountId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<WalletReconcileDetailDto>(wallets.Count);
        var discrepancies = 0;

        foreach (var wallet in wallets)
        {
            snapshots.TryGetValue(wallet.Id, out var snap);
            entriesByWallet.TryGetValue(wallet.Id, out var walletEntries);
            walletEntries ??= new List<Domain.LedgerEntry>();

            var calculatedCredits = walletEntries.Where(e => !e.IsDebit).Sum(e => e.Amount);
            var calculatedDebits = walletEntries.Where(e => e.IsDebit).Sum(e => e.Amount);
            var calculatedBalance = calculatedCredits - calculatedDebits;

            var snapshotBalance = snap?.AvailableBalance ?? 0;
            var isMatched = calculatedBalance == snapshotBalance;

            if (!isMatched)
                discrepancies++;

            results.Add(new WalletReconcileDetailDto(
                wallet.Id,
                wallet.AccountNumber,
                snapshotBalance,
                calculatedBalance,
                calculatedDebits,
                calculatedCredits,
                isMatched,
                Math.Abs(snapshotBalance - calculatedBalance)));
        }

        return new ReconcileLedgerResponse(
            wallets.Count,
            discrepancies,
            discrepancies == 0,
            DateTime.UtcNow,
            results);
    }
}
