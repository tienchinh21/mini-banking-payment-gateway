using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Accounts.Application.TopUpWallet;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Accounts.Endpoints;

public sealed record FreezeWalletRequest(string? Reason);

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        // Public Account Endpoints
        var accountGroup = routes.MapGroup("/api/v1/accounts").WithTags("Accounts");

        accountGroup.MapGet("/{id:guid}/balance", async (Guid id, MiniBankingDbContext db) =>
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

        // Admin Wallet / Account Management Endpoints
        var adminWalletGroup = routes.MapGroup("/api/v1/admin/wallets")
            .RequireAuthorization("Admin")
            .WithTags("Admin - Wallets");

        adminWalletGroup.MapGet("/", async (
            int? page,
            int? pageSize,
            string? search,
            string? currency,
            MiniBankingDbContext db) =>
        {
            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.WalletAccounts
                .Include(w => w.Customer)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(w =>
                    w.AccountNumber.ToLower().Contains(s) ||
                    w.Customer.FullName.ToLower().Contains(s) ||
                    w.Customer.Email.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(currency))
                query = query.Where(w => w.Currency == currency.ToUpper());

            var total = await query.CountAsync();
            var wallets = await query
                .OrderByDescending(w => w.CreatedAt)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();

            var walletIds = wallets.Select(w => w.Id).ToList();
            var snapshots = await db.BalanceSnapshots
                .AsNoTracking()
                .Where(b => walletIds.Contains(b.WalletAccountId))
                .ToDictionaryAsync(b => b.WalletAccountId);

            var items = wallets.Select(w =>
            {
                snapshots.TryGetValue(w.Id, out var snap);
                return new
                {
                    WalletId = w.Id,
                    w.AccountNumber,
                    w.Currency,
                    CustomerId = w.CustomerId,
                    CustomerName = w.Customer.FullName,
                    CustomerEmail = w.Customer.Email,
                    CustomerPhone = w.Customer.PhoneNumber,
                    AvailableBalance = snap?.AvailableBalance ?? 0,
                    LedgerBalance = snap?.LedgerBalance ?? 0,
                    Status = "Active",
                    w.CreatedAt,
                    w.UpdatedAt
                };
            });

            return Results.Ok(ApiResponse.Ok("Danh sách ví tài khoản", new
            {
                Items = items,
                TotalCount = total,
                Page = p,
                PageSize = ps,
                TotalPages = (int)Math.Ceiling(total / (double)ps)
            }));
        });

        adminWalletGroup.MapGet("/{id}", async (string id, MiniBankingDbContext db) =>
        {
            WalletAccount? wallet;
            if (Guid.TryParse(id, out var walletGuid))
            {
                wallet = await db.WalletAccounts
                    .Include(w => w.Customer)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == walletGuid);
            }
            else
            {
                wallet = await db.WalletAccounts
                    .Include(w => w.Customer)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.AccountNumber == id);
            }

            if (wallet is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy ví tài khoản."));

            var snap = await db.BalanceSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.WalletAccountId == wallet.Id);

            var recentEntries = await db.LedgerEntries
                .AsNoTracking()
                .Where(e => e.AccountId == wallet.Id)
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync();

            return Results.Ok(ApiResponse.Ok("Chi tiết ví tài khoản", new
            {
                WalletId = wallet.Id,
                wallet.AccountNumber,
                wallet.Currency,
                CustomerId = wallet.CustomerId,
                CustomerName = wallet.Customer.FullName,
                CustomerEmail = wallet.Customer.Email,
                CustomerPhone = wallet.Customer.PhoneNumber,
                AvailableBalance = snap?.AvailableBalance ?? 0,
                LedgerBalance = snap?.LedgerBalance ?? 0,
                Status = "Active",
                wallet.CreatedAt,
                wallet.UpdatedAt,
                RecentTransactions = recentEntries.Select(e => new
                {
                    e.Id,
                    TransactionId = e.LedgerTransactionId,
                    e.Amount,
                    e.Currency,
                    e.IsDebit,
                    e.CreatedAt
                })
            }));
        });

        adminWalletGroup.MapGet("/{accountNumber}/balance", async (string accountNumber, MiniBankingDbContext db) =>
        {
            var wallet = await db.WalletAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.AccountNumber == accountNumber);

            if (wallet is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy ví tài khoản."));

            var snap = await db.BalanceSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.WalletAccountId == wallet.Id);

            return Results.Ok(ApiResponse.Ok("Số dư ví", new
            {
                wallet.AccountNumber,
                wallet.Currency,
                AvailableBalance = snap?.AvailableBalance ?? 0,
                LedgerBalance = snap?.LedgerBalance ?? 0
            }));
        });

        adminWalletGroup.MapGet("/{accountNumber}/ledger", async (string accountNumber, MiniBankingDbContext db) =>
        {
            var wallet = await db.WalletAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.AccountNumber == accountNumber);

            if (wallet is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy ví tài khoản."));

            var entries = await db.LedgerEntries
                .AsNoTracking()
                .Where(e => e.AccountId == wallet.Id)
                .OrderByDescending(e => e.CreatedAt)
                .Take(50)
                .ToListAsync();

            return Results.Ok(ApiResponse.Ok("Lịch sử biến động số dư", entries.Select(e => new
            {
                e.Id,
                TransactionId = e.LedgerTransactionId,
                e.Amount,
                e.Currency,
                e.IsDebit,
                e.CreatedAt
            })));
        });

        adminWalletGroup.MapPost("/top-up", async (TopUpWalletRequest request, IMediator mediator) =>
        {
            try
            {
                var command = new TopUpWalletCommand(request);
                var response = await mediator.Send(command);
                return Results.Ok(ApiResponse.Ok("Nạp tiền vào ví thành công", response));
            }
            catch (FluentValidation.ValidationException ex)
            {
                return Results.BadRequest(ApiResponse.Fail(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ApiResponse.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Fail($"Nạp tiền thất bại: {ex.Message}"));
            }
        });

        adminWalletGroup.MapPost("/{id}/freeze", (string id, FreezeWalletRequest? req) =>
        {
            return Results.Ok(ApiResponse.Ok("Khóa ví thành công (mô phỏng)", new
            {
                WalletId = id,
                Status = "Frozen",
                Reason = req?.Reason ?? "Admin action",
                FrozenAt = DateTime.UtcNow
            }));
        });

        return routes;
    }
}
