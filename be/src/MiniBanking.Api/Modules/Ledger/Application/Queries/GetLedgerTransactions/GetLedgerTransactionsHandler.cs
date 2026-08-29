using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Ledger.Domain;

namespace MiniBanking.Modules.Ledger.Application.Queries.GetLedgerTransactions;

public sealed class GetLedgerTransactionsHandler : IRequestHandler<GetLedgerTransactionsQuery, GetLedgerTransactionsResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetLedgerTransactionsHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetLedgerTransactionsResponse> Handle(
        GetLedgerTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        var p = Math.Max(1, query.Page ?? 1);
        var ps = Math.Clamp(query.PageSize ?? 20, 1, 100);

        var queryable = _dbContext.LedgerTransactions
            .Include(t => t.Entries)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Type) && Enum.TryParse<LedgerTransactionType>(query.Type, true, out var t))
            queryable = queryable.Where(x => x.Type == t);

        var total = await queryable.CountAsync(cancellationToken);
        var txs = await queryable
            .OrderByDescending(x => x.CreatedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var items = txs.Select(tx =>
        {
            var totalDebit = tx.Entries.Where(e => e.IsDebit).Sum(e => e.Amount);
            var totalCredit = tx.Entries.Where(e => !e.IsDebit).Sum(e => e.Amount);
            var isBalanced = totalDebit == totalCredit;

            var entries = tx.Entries.Select(e => new LedgerTransactionEntrySummaryDto(
                e.Id,
                e.AccountId,
                e.AccountType,
                e.Amount,
                e.Currency,
                e.IsDebit,
                e.Sequence)).ToList();

            return new LedgerTransactionListItemDto(
                tx.Id,
                tx.ReferenceId,
                tx.Type.ToString(),
                tx.Status.ToString(),
                tx.Description,
                tx.CreatedAt,
                tx.Entries.Count,
                totalDebit,
                totalCredit,
                isBalanced,
                entries);
        }).ToList();

        var totalPages = (int)Math.Ceiling(total / (double)ps);

        return new GetLedgerTransactionsResponse(items, total, p, ps, totalPages);
    }
}
