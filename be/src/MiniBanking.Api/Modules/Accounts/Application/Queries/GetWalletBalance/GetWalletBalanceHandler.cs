using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;

namespace MiniBanking.Modules.Accounts.Application.Queries.GetWalletBalance;

public sealed class GetWalletBalanceHandler : IRequestHandler<GetWalletBalanceQuery, WalletBalanceResponse?>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetWalletBalanceHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WalletBalanceResponse?> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        var balance = await _dbContext.BalanceSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.WalletAccountId == request.WalletAccountId, cancellationToken);

        if (balance is null)
            return null;

        return new WalletBalanceResponse(
            balance.WalletAccountId,
            balance.AvailableBalance,
            balance.LedgerBalance,
            balance.Currency,
            balance.Version);
    }
}
