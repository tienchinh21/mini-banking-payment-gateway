using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Ledger.Endpoints;

public static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder routes)
    {
        // 1. Public Ledger Endpoints
        var publicGroup = routes.MapGroup("/api/v1/ledger").WithTags("Ledger");

        publicGroup.MapGet("/transactions/{id:guid}", async (Guid id, MiniBankingDbContext db) =>
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

        // 2. Admin Ledger Management Endpoints
        var adminGroup = routes.MapGroup("/api/v1/admin/ledger")
            .RequireAuthorization("Admin")
            .WithTags("Admin - Ledger");

        adminGroup.MapGet("/entries", async (
            int? page,
            int? pageSize,
            Guid? accountId,
            string? currency,
            bool? isDebit,
            DateTime? fromDate,
            DateTime? toDate,
            MiniBankingDbContext db) =>
        {
            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.LedgerEntries.AsNoTracking().AsQueryable();

            if (accountId.HasValue)
                query = query.Where(e => e.AccountId == accountId.Value);

            if (!string.IsNullOrWhiteSpace(currency))
                query = query.Where(e => e.Currency == currency.ToUpper());

            if (isDebit.HasValue)
                query = query.Where(e => e.IsDebit == isDebit.Value);

            if (fromDate.HasValue)
                query = query.Where(e => e.CreatedAt >= fromDate.Value.ToUniversalTime());

            if (toDate.HasValue)
                query = query.Where(e => e.CreatedAt <= toDate.Value.ToUniversalTime());

            var total = await query.CountAsync();
            var entries = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();

            var txIds = entries.Select(e => e.LedgerTransactionId).Distinct().ToList();
            var txs = await db.LedgerTransactions
                .AsNoTracking()
                .Where(t => txIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id);

            var walletIds = entries.Select(e => e.AccountId).Distinct().ToList();
            var wallets = await db.WalletAccounts
                .Include(w => w.Customer)
                .AsNoTracking()
                .Where(w => walletIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id);

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

                return new
                {
                    EntryId = e.Id,
                    TransactionId = e.LedgerTransactionId,
                    TransactionReference = tx?.ReferenceId,
                    TransactionType = tx?.Type.ToString(),
                    TransactionDescription = tx?.Description,
                    e.AccountId,
                    AccountType = e.AccountType,
                    AccountDisplay = accountDisplay,
                    e.Amount,
                    e.Currency,
                    e.IsDebit,
                    TypeDisplay = e.IsDebit ? "Debit (Nợ)" : "Credit (Có)",
                    e.Sequence,
                    e.CreatedAt
                };
            });

            return Results.Ok(ApiResponse.Ok("Danh sách bút toán sổ cái", new
            {
                Items = items,
                TotalCount = total,
                Page = p,
                PageSize = ps,
                TotalPages = (int)Math.Ceiling(total / (double)ps)
            }));
        });

        adminGroup.MapGet("/transactions", async (
            int? page,
            int? pageSize,
            string? type,
            MiniBankingDbContext db) =>
        {
            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.LedgerTransactions
                .Include(t => t.Entries)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<LedgerTransactionType>(type, true, out var t))
                query = query.Where(x => x.Type == t);

            var total = await query.CountAsync();
            var txs = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();

            var items = txs.Select(t => new
            {
                TransactionId = t.Id,
                t.ReferenceId,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                t.Description,
                t.CreatedAt,
                EntriesCount = t.Entries.Count,
                TotalDebit = t.Entries.Where(e => e.IsDebit).Sum(e => e.Amount),
                TotalCredit = t.Entries.Where(e => !e.IsDebit).Sum(e => e.Amount),
                IsBalanced = t.Entries.Where(e => e.IsDebit).Sum(e => e.Amount) == t.Entries.Where(e => !e.IsDebit).Sum(e => e.Amount),
                Entries = t.Entries.Select(e => new
                {
                    e.Id,
                    e.AccountId,
                    e.AccountType,
                    e.Amount,
                    e.Currency,
                    e.IsDebit,
                    e.Sequence
                })
            });

            return Results.Ok(ApiResponse.Ok("Danh sách giao dịch sổ cái", new
            {
                Items = items,
                TotalCount = total,
                Page = p,
                PageSize = ps,
                TotalPages = (int)Math.Ceiling(total / (double)ps)
            }));
        });

        adminGroup.MapPost("/reconcile", async (MiniBankingDbContext db) =>
        {
            var wallets = await db.WalletAccounts.AsNoTracking().ToListAsync();
            var results = new List<object>();
            var discrepancies = 0;

            foreach (var wallet in wallets)
            {
                var snap = await db.BalanceSnapshots
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.WalletAccountId == wallet.Id);

                var entries = await db.LedgerEntries
                    .AsNoTracking()
                    .Where(e => e.AccountId == wallet.Id)
                    .ToListAsync();

                var calculatedCredits = entries.Where(e => !e.IsDebit).Sum(e => e.Amount);
                var calculatedDebits = entries.Where(e => e.IsDebit).Sum(e => e.Amount);
                var calculatedBalance = calculatedCredits - calculatedDebits;

                var snapshotBalance = snap?.AvailableBalance ?? 0;
                var isMatched = calculatedBalance == snapshotBalance;

                if (!isMatched) discrepancies++;

                results.Add(new
                {
                    WalletId = wallet.Id,
                    wallet.AccountNumber,
                    SnapshotBalance = snapshotBalance,
                    CalculatedBalance = calculatedBalance,
                    TotalDebits = calculatedDebits,
                    TotalCredits = calculatedCredits,
                    IsMatched = isMatched,
                    Difference = Math.Abs(snapshotBalance - calculatedBalance)
                });
            }

            return Results.Ok(ApiResponse.Ok("Kết quả đối soát số dư", new
            {
                TotalWallets = wallets.Count,
                DiscrepanciesCount = discrepancies,
                IsFullyReconciled = discrepancies == 0,
                ReconciledAt = DateTime.UtcNow,
                Details = results
            }));
        });

        return routes;
    }
}
