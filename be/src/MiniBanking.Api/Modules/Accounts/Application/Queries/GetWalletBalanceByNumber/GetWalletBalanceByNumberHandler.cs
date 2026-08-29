using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;

namespace MiniBanking.Modules.Accounts.Application.Queries.GetWalletBalanceByNumber;

public sealed class GetWalletBalanceByNumberHandler : IRequestHandler<GetWalletBalanceByNumberQuery, WalletBalanceByNumberResponse?>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetWalletBalanceByNumberHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WalletBalanceByNumberResponse?> Handle(GetWalletBalanceByNumberQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.WalletAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.AccountNumber == request.AccountNumber, cancellationToken);

        if (wallet is null)
            return null;

        var snap = await _dbContext.BalanceSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.WalletAccountId == wallet.Id, cancellationToken);

        return new WalletBalanceByNumberResponse(
            wallet.AccountNumber,
            wallet.Currency,
            snap?.AvailableBalance ?? 0,
            snap?.LedgerBalance ?? 0);
    }
}
