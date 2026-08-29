using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;

namespace MiniBanking.Modules.Accounts.Application.Queries.GetWalletLedger;

public sealed class GetWalletLedgerHandler : IRequestHandler<GetWalletLedgerQuery, IReadOnlyList<WalletLedgerEntryDto>?>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetWalletLedgerHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WalletLedgerEntryDto>?> Handle(GetWalletLedgerQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.WalletAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.AccountNumber == request.AccountNumber, cancellationToken);

        if (wallet is null)
            return null;

        var entries = await _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => e.AccountId == wallet.Id)
            .OrderByDescending(e => e.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return entries.Select(e => new WalletLedgerEntryDto(
            e.Id,
            e.LedgerTransactionId,
            e.Amount,
            e.Currency,
            e.IsDebit,
            e.CreatedAt)).ToList();
    }
}
