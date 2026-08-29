using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;

namespace MiniBanking.Modules.Accounts.Application.Queries.GetWallets;

public sealed class GetWalletsHandler : IRequestHandler<GetWalletsQuery, WalletsListResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetWalletsHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WalletsListResponse> Handle(GetWalletsQuery request, CancellationToken cancellationToken)
    {
        var p = Math.Max(1, request.Page ?? 1);
        var ps = Math.Clamp(request.PageSize ?? 20, 1, 100);

        var query = _dbContext.WalletAccounts
            .Include(w => w.Customer)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(w =>
                w.AccountNumber.ToLower().Contains(s) ||
                w.Customer.FullName.ToLower().Contains(s) ||
                w.Customer.Email.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            var currencyUpper = request.Currency.ToUpper();
            query = query.Where(w => w.Currency == currencyUpper);
        }

        var total = await query.CountAsync(cancellationToken);
        var wallets = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var walletIds = wallets.Select(w => w.Id).ToList();
        var snapshots = await _dbContext.BalanceSnapshots
            .AsNoTracking()
            .Where(b => walletIds.Contains(b.WalletAccountId))
            .ToDictionaryAsync(b => b.WalletAccountId, cancellationToken);

        var items = wallets.Select(w =>
        {
            snapshots.TryGetValue(w.Id, out var snap);
            return new WalletSummaryDto(
                w.Id,
                w.AccountNumber,
                w.Currency,
                w.CustomerId,
                w.Customer.FullName,
                w.Customer.Email,
                w.Customer.PhoneNumber,
                snap?.AvailableBalance ?? 0,
                snap?.LedgerBalance ?? 0,
                "Active",
                w.CreatedAt,
                w.UpdatedAt);
        }).ToList();

        var totalPages = (int)Math.Ceiling(total / (double)ps);

        return new WalletsListResponse(items, total, p, ps, totalPages);
    }
}
