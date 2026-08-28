using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Ledger.Endpoints;

public static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/ledger").WithTags("Ledger");

        group.MapGet("/transactions/{id:guid}", async (Guid id, MiniBankingDbContext db) =>
        {
            var tx = await db.LedgerTransactions
                .Include(t => t.Entries)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tx is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy giao dịch sổ cái."));

            return Results.Ok(ApiResponse.Ok("Chi tiết giao dịch sổ cái", new
            {
                TransactionId = tx.Id,
                ReferenceId = tx.ReferenceId,
                Type = tx.Type.ToString(),
                Status = tx.Status.ToString(),
                Description = tx.Description,
                Entries = tx.Entries.Select(e => new
                {
                    EntryId = e.Id,
                    AccountId = e.AccountId,
                    AccountType = e.AccountType,
                    Amount = e.Amount,
                    Currency = e.Currency,
                    IsDebit = e.IsDebit
                })
            }));
        });

        return routes;
    }
}
