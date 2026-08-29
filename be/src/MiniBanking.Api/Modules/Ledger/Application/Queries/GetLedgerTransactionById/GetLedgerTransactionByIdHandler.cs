using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;

namespace MiniBanking.Modules.Ledger.Application.Queries.GetLedgerTransactionById;

public sealed class GetLedgerTransactionByIdHandler : IRequestHandler<GetLedgerTransactionByIdQuery, GetLedgerTransactionByIdResponse?>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetLedgerTransactionByIdHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetLedgerTransactionByIdResponse?> Handle(
        GetLedgerTransactionByIdQuery query,
        CancellationToken cancellationToken)
    {
        var tx = await _dbContext.LedgerTransactions
            .Include(t => t.Entries)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == query.Id, cancellationToken);

        if (tx is null)
            return null;

        var entries = tx.Entries.Select(e => new LedgerTransactionEntryDto(
            e.Id,
            e.AccountId,
            e.AccountType,
            e.Amount,
            e.Currency,
            e.IsDebit));

        return new GetLedgerTransactionByIdResponse(
            tx.Id,
            tx.ReferenceId,
            tx.Type.ToString(),
            tx.Status.ToString(),
            tx.Description,
            entries);
    }
}
