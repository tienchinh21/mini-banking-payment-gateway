using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Accounts.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/accounts").WithTags("Accounts");

        group.MapGet("/{id:guid}/balance", async (Guid id, MiniBankingDbContext db) =>
        {
            var balance = await db.BalanceSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.WalletAccountId == id);

            if (balance is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy ví tài khoản."));

            return Results.Ok(ApiResponse.Ok("Thông tin số dư ví", new
            {
                WalletAccountId = balance.WalletAccountId,
                AvailableBalance = balance.AvailableBalance,
                LedgerBalance = balance.LedgerBalance,
                Currency = balance.Currency,
                Version = balance.Version
            }));
        });

        return routes;
    }
}
