using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Accounts.Domain;

namespace MiniBanking.Modules.Accounts.Application.Queries.GetWalletDetail;

public sealed class GetWalletDetailHandler : IRequestHandler<GetWalletDetailQuery, WalletDetailResponse?>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetWalletDetailHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WalletDetailResponse?> Handle(GetWalletDetailQuery request, CancellationToken cancellationToken)
    {
        WalletAccount? wallet;
        if (Guid.TryParse(request.Identifier, out var walletGuid))
        {
            wallet = await _dbContext.WalletAccounts
                .Include(w => w.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == walletGuid, cancellationToken);
        }
        else
        {
            wallet = await _dbContext.WalletAccounts
                .Include(w => w.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.AccountNumber == request.Identifier, cancellationToken);
        }

        if (wallet is null)
            return null;

        var snap = await _dbContext.BalanceSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.WalletAccountId == wallet.Id, cancellationToken);

        var recentEntries = await _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => e.AccountId == wallet.Id)
            .OrderByDescending(e => e.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentTransactions = recentEntries.Select(e => new RecentTransactionDto(
            e.Id,
            e.LedgerTransactionId,
            e.Amount,
            e.Currency,
            e.IsDebit,
            e.CreatedAt)).ToList();

        return new WalletDetailResponse(
            wallet.Id,
            wallet.AccountNumber,
            wallet.Currency,
            wallet.CustomerId,
            wallet.Customer.FullName,
            wallet.Customer.Email,
            wallet.Customer.PhoneNumber,
            snap?.AvailableBalance ?? 0,
            snap?.LedgerBalance ?? 0,
            "Active",
            wallet.CreatedAt,
            wallet.UpdatedAt,
            recentTransactions);
    }
}
