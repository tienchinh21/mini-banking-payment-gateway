using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Ledger.Application.Queries.GetLedgerEntries;

public sealed class GetLedgerEntriesHandler : IRequestHandler<GetLedgerEntriesQuery, GetLedgerEntriesResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetLedgerEntriesHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetLedgerEntriesResponse> Handle(
        GetLedgerEntriesQuery query,
        CancellationToken cancellationToken)
    {
        var p = Math.Max(1, query.Page ?? 1);
        var ps = Math.Clamp(query.PageSize ?? 20, 1, 100);

        var queryable = _dbContext.LedgerEntries.AsNoTracking().AsQueryable();

        if (query.AccountId.HasValue)
            queryable = queryable.Where(e => e.AccountId == query.AccountId.Value);

        if (!string.IsNullOrWhiteSpace(query.Currency))
            queryable = queryable.Where(e => e.Currency == query.Currency.ToUpper());

        if (query.IsDebit.HasValue)
            queryable = queryable.Where(e => e.IsDebit == query.IsDebit.Value);

        if (query.FromDate.HasValue)
            queryable = queryable.Where(e => e.CreatedAt >= query.FromDate.Value.ToUniversalTime());

        if (query.ToDate.HasValue)
            queryable = queryable.Where(e => e.CreatedAt <= query.ToDate.Value.ToUniversalTime());

        var total = await queryable.CountAsync(cancellationToken);
        var entries = await queryable
            .OrderByDescending(e => e.CreatedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var txIds = entries.Select(e => e.LedgerTransactionId).Distinct().ToList();
        var txs = await _dbContext.LedgerTransactions
            .AsNoTracking()
            .Where(t => txIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var walletIds = entries.Select(e => e.AccountId).Distinct().ToList();
        var wallets = await _dbContext.WalletAccounts
            .Include(w => w.Customer)
            .AsNoTracking()
            .Where(w => walletIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, cancellationToken);

        var items = entries.Select(e =>
        {
            txs.TryGetValue(e.LedgerTransactionId, out var tx);
            wallets.TryGetValue(e.AccountId, out var w);

            string accountDisplay;
            if (e.AccountId == SystemAccountIds.PlatformClearing)
                accountDisplay = "Platform Clearing Account";
            else if (e.AccountId == SystemAccountIds.MerchantSettlement)
                accountDisplay = "Merchant Settlement Account";
            else if (e.AccountId == SystemAccountIds.PlatformFee)
                accountDisplay = "Platform Fee Account";
            else if (w is not null)
                accountDisplay = $"{w.AccountNumber} ({w.Customer.FullName})";
            else
                accountDisplay = e.AccountId.ToString();

            return new LedgerEntryItemDto(
                e.Id,
                e.LedgerTransactionId,
                tx?.ReferenceId,
                tx?.Type.ToString(),
                tx?.Description,
                e.AccountId,
                e.AccountType,
                accountDisplay,
                e.Amount,
                e.Currency,
                e.IsDebit,
                e.IsDebit ? "Debit (Nợ)" : "Credit (Có)",
                e.Sequence,
                e.CreatedAt);
        }).ToList();

        var totalPages = (int)Math.Ceiling(total / (double)ps);

        return new GetLedgerEntriesResponse(items, total, p, ps, totalPages);
    }
}
