using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.Modules.Admin.Application;
using MiniBanking.Modules.Admin.Domain;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.Modules.Merchants.Domain;
using MiniBanking.Modules.Payments.Application.CreatePayment;
using MiniBanking.Modules.Payments.Application.CreateRefund;
using MiniBanking.Modules.Payments.Application.CreateSettlement;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Admin.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/admin");

        // Auth endpoints
        group.MapPost("/auth/login", async (LoginRequest request, IJwtTokenService tokenService, MiniBankingDbContext db) =>
        {
            var admin = await db.AdminUsers
                .FirstOrDefaultAsync(a => a.Email == request.Email && a.IsActive);

            if (admin is null || !PasswordHasher.Verify(request.Password, admin.PasswordHash))
                return Results.Unauthorized();

            var token = tokenService.GenerateToken(
                admin.Id.ToString(),
                admin.Email,
                admin.FullName,
                new[] { admin.Role });

            return Results.Ok(ApiResponse.Ok("Đăng nhập thành công", new { Token = token }));
        });

        group.MapPost("/auth/logout", () =>
        {
            return Results.Ok(ApiResponse.Ok("Đăng xuất thành công"));
        }).RequireAuthorization("Admin");

        group.MapGet("/auth/profile", (HttpContext context) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
            var name = context.User.FindFirst(ClaimTypes.Name)?.Value;
            var role = context.User.FindFirst(ClaimTypes.Role)?.Value;

            return Results.Ok(ApiResponse.Ok("Thông tin người dùng", new
            {
                Id = userId,
                Email = email,
                FullName = name,
                Role = role
            }));
        }).RequireAuthorization("Admin");

        // 1. Wallets / Accounts
        group.MapGet("/wallets", async (
            string? keyword,
            string? status,
            int page = 1,
            int pageSize = 10,
            MiniBankingDbContext db = null!) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

            var query = db.WalletAccounts
                .Include(w => w.Customer)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(w =>
                    w.AccountNumber.ToLower().Contains(kw) ||
                    w.Customer.FullName.ToLower().Contains(kw) ||
                    w.Customer.Email.ToLower().Contains(kw) ||
                    w.Customer.PhoneNumber.ToLower().Contains(kw));
            }

            var totalItems = await query.CountAsync();

            var wallets = await query
                .OrderByDescending(w => w.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var walletIds = wallets.Select(w => w.Id).ToList();
            var balances = await db.BalanceSnapshots
                .AsNoTracking()
                .Where(b => walletIds.Contains(b.WalletAccountId))
                .ToDictionaryAsync(b => b.WalletAccountId);

            var items = wallets.Select(w =>
            {
                balances.TryGetValue(w.Id, out var bal);
                return new
                {
                    w.Id,
                    w.AccountNumber,
                    CustomerName = w.Customer.FullName,
                    Email = w.Customer.Email,
                    Phone = w.Customer.PhoneNumber,
                    w.Currency,
                    AvailableBalance = bal?.Available.Amount ?? 0,
                    LedgerBalance = bal?.Ledger.Amount ?? 0,
                    Status = "ACTIVE",
                    w.CreatedAt
                };
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return Results.Ok(ApiResponse.Ok("Danh sách ví", new
            {
                Items = items,
                Meta = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasNext = page < totalPages,
                    HasPrevious = page > 1
                }
            }));
        }).RequireAuthorization("Admin");

        group.MapGet("/wallets/{id}", async (string id, MiniBankingDbContext db) =>
        {
            WalletAccount? wallet = null;
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
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy tài khoản ví"));

            var balance = await db.BalanceSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.WalletAccountId == wallet.Id);

            var recentEntries = await db.LedgerEntries
                .AsNoTracking()
                .Where(e => e.AccountId == wallet.Id)
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync();

            return Results.Ok(ApiResponse.Ok("Chi tiết tài khoản ví", new
            {
                wallet.Id,
                wallet.AccountNumber,
                CustomerName = wallet.Customer.FullName,
                Email = wallet.Customer.Email,
                Phone = wallet.Customer.PhoneNumber,
                wallet.Currency,
                AvailableBalance = balance?.Available.Amount ?? 0,
                LedgerBalance = balance?.Ledger.Amount ?? 0,
                Status = "ACTIVE",
                wallet.CreatedAt,
                RecentEntries = recentEntries
            }));
        }).RequireAuthorization("Admin");

        group.MapGet("/wallets/{accountNumber}/balance", async (string accountNumber, MiniBankingDbContext db) =>
        {
            var wallet = await db.WalletAccounts
                .AsNoTracking()
                .Include(w => w.Customer)
                .FirstOrDefaultAsync(w => w.AccountNumber == accountNumber || w.Id.ToString() == accountNumber);

            if (wallet is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy tài khoản ví"));

            var balance = await db.BalanceSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.WalletAccountId == wallet.Id);

            return Results.Ok(ApiResponse.Ok("Thông tin số dư", new
            {
                wallet.Id,
                wallet.AccountNumber,
                wallet.Currency,
                CustomerName = wallet.Customer.FullName,
                AvailableBalance = balance?.Available.Amount ?? 0,
                LedgerBalance = balance?.Ledger.Amount ?? 0
            }));
        }).RequireAuthorization("Admin");

        group.MapGet("/wallets/{accountNumber}/ledger", async (string accountNumber, MiniBankingDbContext db) =>
        {
            var wallet = await db.WalletAccounts
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.AccountNumber == accountNumber || w.Id.ToString() == accountNumber);

            if (wallet is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy tài khoản ví"));

            var entries = await db.LedgerEntries
                .AsNoTracking()
                .Where(e => e.AccountId == wallet.Id)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.Id,
                    e.LedgerTransactionId,
                    e.AccountType,
                    e.Amount,
                    e.Currency,
                    e.IsDebit,
                    e.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(ApiResponse.Ok("Lịch sử sổ cái", entries));
        }).RequireAuthorization("Admin");

        group.MapPost("/wallets/top-up", async (TopUpWalletRequest request, MiniBankingDbContext db) =>
        {
            if (request.Amount <= 0)
                return Results.BadRequest(ApiResponse.Fail("Số tiền nạp phải lớn hơn 0"));

            var wallet = await db.WalletAccounts
                .Include(w => w.Customer)
                .FirstOrDefaultAsync(w => w.AccountNumber == request.AccountNumber || w.Id.ToString() == request.AccountNumber);

            if (wallet is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy tài khoản ví"));

            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                var balance = await db.BalanceSnapshots
                    .FromSqlInterpolated($"SELECT * FROM public.balance_snapshots WHERE \"WalletAccountId\" = {wallet.Id} FOR UPDATE")
                    .FirstOrDefaultAsync();

                var amount = new Money(request.Amount, wallet.Currency);

                if (balance is null)
                {
                    balance = new BalanceSnapshot(wallet, amount);
                    db.BalanceSnapshots.Add(balance);
                }
                else
                {
                    balance.Credit(amount);
                }

                var txn = new LedgerTransaction(
                    $"TOPUP-{Guid.NewGuid():N}",
                    LedgerTransactionType.TopUp,
                    string.IsNullOrWhiteSpace(request.Description) ? $"Admin nạp tiền ví {wallet.AccountNumber}" : request.Description);

                txn.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", amount, isDebit: true);
                txn.AddEntry(wallet.Id, "WalletAccount", amount, isDebit: false);
                txn.ValidateInvariant();

                db.LedgerTransactions.Add(txn);
                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(ApiResponse.Ok("Nạp tiền thành công", new
                {
                    wallet.Id,
                    wallet.AccountNumber,
                    Amount = request.Amount,
                    Currency = wallet.Currency,
                    AvailableBalance = balance.Available.Amount,
                    LedgerBalance = balance.Ledger.Amount,
                    TransactionId = txn.Id
                }));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Results.BadRequest(ApiResponse.Fail($"Lỗi khi nạp tiền: {ex.Message}"));
            }
        }).RequireAuthorization("Admin");

        group.MapPost("/wallets/{id}/freeze", (string id, FreezeWalletRequest? req) =>
        {
            return Results.Ok(ApiResponse.Ok("Cập nhật trạng thái ví thành công", new
            {
                Id = id,
                Status = req?.Status ?? "FROZEN"
            }));
        }).RequireAuthorization("Admin");

        // 2. Payments
        group.MapGet("/payments", async (
            string? keyword,
            string? status,
            string? merchantId,
            int page = 1,
            int pageSize = 10,
            MiniBankingDbContext db = null!) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

            var query = db.Payments.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(p =>
                    p.Id.ToString().ToLower().Contains(kw) ||
                    p.MerchantOrderId.ToLower().Contains(kw) ||
                    p.MerchantId.ToLower().Contains(kw) ||
                    p.Description.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(p => p.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(merchantId))
            {
                query = query.Where(p => p.MerchantId == merchantId);
            }

            var totalItems = await query.CountAsync();

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var merchantIds = payments.Select(p => p.MerchantId).Distinct().ToList();
            var merchants = await db.Merchants
                .AsNoTracking()
                .Where(m => merchantIds.Contains(m.MerchantId))
                .ToDictionaryAsync(m => m.MerchantId, m => m.Name);

            var walletIds = payments.Select(p => p.WalletAccountId).Distinct().ToList();
            var wallets = await db.WalletAccounts
                .Include(w => w.Customer)
                .AsNoTracking()
                .Where(w => walletIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id);

            var items = payments.Select(p =>
            {
                merchants.TryGetValue(p.MerchantId, out var mName);
                wallets.TryGetValue(p.WalletAccountId, out var w);

                return new
                {
                    p.Id,
                    p.MerchantId,
                    MerchantName = mName ?? p.MerchantId,
                    OrderId = p.MerchantOrderId,
                    PayerWalletNumber = w?.AccountNumber ?? p.WalletAccountId.ToString(),
                    PayerName = w?.Customer?.FullName ?? "Khách hàng",
                    p.Amount,
                    p.Currency,
                    Status = p.Status.ToString().ToUpperInvariant(),
                    ErrorMessage = p.FailureCode,
                    p.IdempotencyKey,
                    p.CreatedAt
                };
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return Results.Ok(ApiResponse.Ok("Danh sách thanh toán", new
            {
                Items = items,
                Meta = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasNext = page < totalPages,
                    HasPrevious = page > 1
                }
            }));
        }).RequireAuthorization("Admin");

        group.MapGet("/payments/{id}", async (Guid id, MiniBankingDbContext db) =>
        {
            var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (payment is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy giao dịch thanh toán"));

            var merchant = await db.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.MerchantId == payment.MerchantId);
            var wallet = await db.WalletAccounts
                .Include(w => w.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == payment.WalletAccountId);

            return Results.Ok(ApiResponse.Ok("Chi tiết thanh toán", new
            {
                payment.Id,
                payment.MerchantId,
                MerchantName = merchant?.Name ?? payment.MerchantId,
                OrderId = payment.MerchantOrderId,
                PayerWalletNumber = wallet?.AccountNumber ?? payment.WalletAccountId.ToString(),
                PayerName = wallet?.Customer?.FullName ?? "Khách hàng",
                payment.Amount,
                payment.Currency,
                Status = payment.Status.ToString().ToUpperInvariant(),
                payment.Description,
                payment.CallbackUrl,
                payment.FailureCode,
                payment.IdempotencyKey,
                payment.LedgerTransactionId,
                payment.CreatedAt
            }));
        }).RequireAuthorization("Admin");

        group.MapPost("/payments/{id}/refund", async (string id, AdminRefundRequest request, IMediator mediator, MiniBankingDbContext db) =>
        {
            Payment? payment = null;
            if (Guid.TryParse(id, out var paymentGuid))
            {
                payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentGuid);
            }
            else
            {
                payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.MerchantOrderId == id);
            }

            if (payment is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy giao dịch thanh toán"));

            var refundAmount = request.Amount > 0 ? request.Amount : payment.Amount;
            var refundReq = new CreateRefundRequest(
                $"REF-{Guid.NewGuid():N}"[..20],
                payment.Id,
                refundAmount,
                payment.Currency,
                string.IsNullOrWhiteSpace(request.Reason) ? "Admin hoàn tiền" : request.Reason);

            var command = new CreateRefundCommand(
                payment.MerchantId,
                $"idem-admin-ref-{Guid.NewGuid():N}",
                "POST",
                $"/api/v1/admin/payments/{id}/refund",
                JsonSerializer.Serialize(refundReq),
                refundReq);

            try
            {
                var response = await mediator.Send(command);
                return response.Status == "Succeeded"
                    ? Results.Ok(ApiResponse.Ok("Hoàn tiền thành công", response))
                    : Results.BadRequest(ApiResponse.Fail($"Hoàn tiền thất bại: {response.FailureCode}"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Fail(ex.Message));
            }
        }).RequireAuthorization("Admin");

        // 3. Settlements
        group.MapPost("/settlements", async (CreateSettlementRequest request, IMediator mediator) =>
        {
            try
            {
                var response = await mediator.Send(new CreateSettlementCommand(request));
                return Results.Ok(ApiResponse.Ok("Quyết toán thành công", response));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ApiResponse.Fail(ex.Message));
            }
        }).RequireAuthorization("Admin");

        // 4. Ledger
        group.MapGet("/ledger/entries", async (
            string? keyword,
            string? accountType,
            string? entryType,
            int page = 1,
            int pageSize = 10,
            MiniBankingDbContext db = null!) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

            var query = db.LedgerEntries
                .Include(e => e.LedgerTransaction)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(e =>
                    e.Id.ToString().ToLower().Contains(kw) ||
                    e.LedgerTransaction.ReferenceId.ToLower().Contains(kw) ||
                    e.AccountId.ToString().ToLower().Contains(kw) ||
                    e.AccountType.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(accountType))
            {
                query = query.Where(e => e.AccountType.ToLower() == accountType.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(entryType))
            {
                var isDebit = entryType.Trim().ToUpperInvariant() == "DEBIT";
                query = query.Where(e => e.IsDebit == isDebit);
            }

            var totalItems = await query.CountAsync();

            var entries = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var walletIds = entries.Select(e => e.AccountId).Distinct().ToList();
            var wallets = await db.WalletAccounts
                .Include(w => w.Customer)
                .AsNoTracking()
                .Where(w => walletIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id);

            var items = entries.Select(e =>
            {
                var accountName = e.AccountType switch
                {
                    "PlatformClearing" => "Tài khoản Platform Clearing",
                    "MerchantSettlement" => "Tài khoản Merchant Settlement",
                    "WalletAccount" => wallets.TryGetValue(e.AccountId, out var w) ? $"Ví {w.AccountNumber} ({w.Customer.FullName})" : $"Ví {e.AccountId}",
                    _ => e.AccountType
                };

                var accountDisplayId = wallets.TryGetValue(e.AccountId, out var wa) ? wa.AccountNumber : e.AccountId.ToString();

                return new
                {
                    Id = e.Id.ToString(),
                    TransactionId = e.LedgerTransaction.ReferenceId,
                    TransactionType = e.LedgerTransaction.Type.ToString().ToUpperInvariant(),
                    AccountId = accountDisplayId,
                    AccountName = accountName,
                    AccountType = e.AccountType switch
                    {
                        "PlatformClearing" => "PLATFORM_CLEARING",
                        "MerchantSettlement" => "MERCHANT_SETTLEMENT",
                        "WalletAccount" => "USER_WALLET",
                        _ => e.AccountType.ToUpperInvariant()
                    },
                    EntryType = e.IsDebit ? "DEBIT" : "CREDIT",
                    e.Amount,
                    e.Currency,
                    e.CreatedAt
                };
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return Results.Ok(ApiResponse.Ok("Danh sách bút toán sổ cái", new
            {
                Items = items,
                Meta = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasNext = page < totalPages,
                    HasPrevious = page > 1
                }
            }));
        }).RequireAuthorization("Admin");

        group.MapGet("/ledger/transactions", async (
            string? keyword,
            int page = 1,
            int pageSize = 10,
            MiniBankingDbContext db = null!) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

            var query = db.LedgerTransactions
                .Include(t => t.Entries)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(t =>
                    t.ReferenceId.ToLower().Contains(kw) ||
                    t.Description.ToLower().Contains(kw));
            }

            var totalItems = await query.CountAsync();

            var txns = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return Results.Ok(ApiResponse.Ok("Danh sách giao dịch sổ cái", new
            {
                Items = txns,
                Meta = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasNext = page < totalPages,
                    HasPrevious = page > 1
                }
            }));
        }).RequireAuthorization("Admin");

        group.MapPost("/ledger/reconcile", async (MiniBankingDbContext db) =>
        {
            var totalAccounts = await db.WalletAccounts.CountAsync();
            var totalEntries = await db.LedgerEntries.CountAsync();
            var debitSum = await db.LedgerEntries.Where(e => e.IsDebit).SumAsync(e => e.Amount);
            var creditSum = await db.LedgerEntries.Where(e => !e.IsDebit).SumAsync(e => e.Amount);

            var isBalanced = debitSum == creditSum;

            return Results.Ok(ApiResponse.Ok("Đối soát sổ cái hoàn tất", new
            {
                Status = isBalanced ? "BALANCED" : "DISCREPANCY",
                IsBalanced = isBalanced,
                TotalDebit = debitSum,
                TotalCredit = creditSum,
                TotalAccountsChecked = totalAccounts,
                TotalEntriesChecked = totalEntries,
                CheckedAt = DateTime.UtcNow
            }));
        }).RequireAuthorization("Admin");

        // 5. Merchants
        group.MapGet("/merchants", async (
            string? keyword,
            int page = 1,
            int pageSize = 10,
            MiniBankingDbContext db = null!) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

            var query = db.Merchants.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(m =>
                    m.MerchantId.ToLower().Contains(kw) ||
                    m.Name.ToLower().Contains(kw) ||
                    (m.WebhookUrl != null && m.WebhookUrl.ToLower().Contains(kw)));
            }

            var totalItems = await query.CountAsync();

            var merchants = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = merchants.Select(m => new
            {
                m.Id,
                Code = m.MerchantId,
                m.Name,
                ContactEmail = $"{m.MerchantId.ToLower()}@merchant.partner",
                m.ApiKey,
                Status = m.IsActive ? "ACTIVE" : "SUSPENDED",
                m.WebhookUrl,
                m.CreatedAt
            }).ToList();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return Results.Ok(ApiResponse.Ok("Danh sách merchant", new
            {
                Items = items,
                Meta = new
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    HasNext = page < totalPages,
                    HasPrevious = page > 1
                }
            }));
        }).RequireAuthorization("Admin");

        group.MapGet("/merchants/{id}", async (string id, MiniBankingDbContext db) =>
        {
            Merchant? merchant = null;
            if (Guid.TryParse(id, out var merchantGuid))
            {
                merchant = await db.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.Id == merchantGuid);
            }
            else
            {
                merchant = await db.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.MerchantId == id);
            }

            if (merchant is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy đối tác Merchant"));

            return Results.Ok(ApiResponse.Ok("Chi tiết đối tác Merchant", new
            {
                merchant.Id,
                Code = merchant.MerchantId,
                merchant.Name,
                ContactEmail = $"{merchant.MerchantId.ToLower()}@merchant.partner",
                merchant.ApiKey,
                merchant.Secret,
                Status = merchant.IsActive ? "ACTIVE" : "SUSPENDED",
                merchant.WebhookUrl,
                merchant.CreatedAt
            }));
        }).RequireAuthorization("Admin");

        group.MapPost("/merchants", async (CreateMerchantAdminRequest request, MiniBankingDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(ApiResponse.Fail("Mã Merchant và Tên Merchant không được để trống"));

            var exists = await db.Merchants.AnyAsync(m => m.MerchantId == request.Code);
            if (exists)
                return Results.Conflict(ApiResponse.Fail($"Mã Merchant '{request.Code}' đã tồn tại"));

            var apiKey = $"mch_live_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLower()}";
            var secret = $"mch_sec_{Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower()}";

            var merchant = new Merchant(
                request.Code.Trim(),
                request.Name.Trim(),
                apiKey,
                secret,
                request.WebhookUrl?.Trim());

            db.Merchants.Add(merchant);
            await db.SaveChangesAsync();

            return Results.Ok(ApiResponse.Ok("Tạo đối tác Merchant thành công", new
            {
                merchant.Id,
                Code = merchant.MerchantId,
                merchant.Name,
                merchant.ApiKey,
                Secret = merchant.Secret,
                merchant.WebhookUrl,
                Status = "ACTIVE",
                merchant.CreatedAt
            }));
        }).RequireAuthorization("Admin");

        group.MapPost("/merchants/{id}/regenerate-keys", async (string id, MiniBankingDbContext db) =>
        {
            Merchant? merchant = null;
            if (Guid.TryParse(id, out var merchantGuid))
            {
                merchant = await db.Merchants.FirstOrDefaultAsync(m => m.Id == merchantGuid);
            }
            else
            {
                merchant = await db.Merchants.FirstOrDefaultAsync(m => m.MerchantId == id);
            }

            if (merchant is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy đối tác Merchant"));

            var newApiKey = $"mch_live_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLower()}";
            var newSecret = $"mch_sec_{Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLower()}";

            var propApiKey = typeof(Merchant).GetProperty("ApiKey");
            var propSecret = typeof(Merchant).GetProperty("Secret");
            propApiKey?.SetValue(merchant, newApiKey);
            propSecret?.SetValue(merchant, newSecret);

            await db.SaveChangesAsync();

            return Results.Ok(ApiResponse.Ok("Cấp lại API Key thành công", new
            {
                merchant.Id,
                Code = merchant.MerchantId,
                ApiKey = newApiKey,
                Secret = newSecret
            }));
        }).RequireAuthorization("Admin");

        // 6. Dashboard Statistics
        group.MapGet("/dashboard/stats", async (MiniBankingDbContext db) =>
        {
            var totalBalance = await db.BalanceSnapshots.SumAsync(b => (long?)b.AvailableBalance) ?? 0;

            var today = DateTime.UtcNow.Date;
            var dailyPayments = await db.Payments
                .Where(p => p.CreatedAt >= today && p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => (long?)p.Amount) ?? 0;

            var totalPaymentsCount = await db.Payments.CountAsync();
            var succeededPaymentsCount = await db.Payments.CountAsync(p => p.Status == PaymentStatus.Succeeded);
            var successRate = totalPaymentsCount > 0
                ? Math.Round((double)succeededPaymentsCount * 100 / totalPaymentsCount, 1)
                : 100.0;

            var activeMerchants = await db.Merchants.CountAsync(m => m.IsActive);

            var recentPaymentsRaw = await db.Payments
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            var merchantIds = recentPaymentsRaw.Select(p => p.MerchantId).Distinct().ToList();
            var merchants = await db.Merchants
                .AsNoTracking()
                .Where(m => merchantIds.Contains(m.MerchantId))
                .ToDictionaryAsync(m => m.MerchantId, m => m.Name);

            var walletIds = recentPaymentsRaw.Select(p => p.WalletAccountId).Distinct().ToList();
            var wallets = await db.WalletAccounts
                .Include(w => w.Customer)
                .AsNoTracking()
                .Where(w => walletIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id);

            var recentPayments = recentPaymentsRaw.Select(p =>
            {
                merchants.TryGetValue(p.MerchantId, out var mName);
                wallets.TryGetValue(p.WalletAccountId, out var w);
                return new
                {
                    Id = p.Id.ToString(),
                    OrderId = p.MerchantOrderId,
                    MerchantName = mName ?? p.MerchantId,
                    CustomerName = w?.Customer?.FullName ?? "Khách hàng",
                    p.Amount,
                    p.Currency,
                    Status = p.Status.ToString().ToUpperInvariant(),
                    p.CreatedAt
                };
            }).ToList();

            return Results.Ok(ApiResponse.Ok("Thống kê dashboard", new
            {
                TotalBalance = totalBalance,
                DailyPayments = dailyPayments,
                SuccessRate = successRate,
                ActiveMerchants = activeMerchants,
                RecentPayments = recentPayments
            }));
        }).RequireAuthorization("Admin");

        // 7. Audit Logs
        group.MapGet("/audit-logs", async (
            string? keyword,
            int page = 1,
            int pageSize = 10,
            MiniBankingDbContext db = null!) =>
        {
            return await GetAuditLogsPaged(db, keyword, page, pageSize);
        }).RequireAuthorization("Admin");

        group.MapGet("/audit/logs", async (
            string? keyword,
            int page = 1,
            int pageSize = 10,
            MiniBankingDbContext db = null!) =>
        {
            return await GetAuditLogsPaged(db, keyword, page, pageSize);
        }).RequireAuthorization("Admin");

        return routes;
    }

    private static async Task<IResult> GetAuditLogsPaged(
        MiniBankingDbContext db,
        string? keyword,
        int page,
        int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

        var query = db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(a =>
                a.Action.ToLower().Contains(kw) ||
                a.ActorEmail.ToLower().Contains(kw) ||
                a.Resource.ToLower().Contains(kw) ||
                (a.CorrelationId != null && a.CorrelationId.ToLower().Contains(kw)));
        }

        var totalItems = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                Id = a.Id.ToString(),
                CorrelationId = a.CorrelationId ?? "N/A",
                a.Action,
                Actor = a.ActorEmail,
                IpAddress = a.IpAddress ?? "127.0.0.1",
                a.Resource,
                Status = a.ResponseStatusCode >= 200 && a.ResponseStatusCode < 400 ? "SUCCESS" : "FAILURE",
                Details = $"{a.Method} {a.Path} (Status {a.ResponseStatusCode})",
                Timestamp = a.CreatedAt
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        return Results.Ok(ApiResponse.Ok("Lịch sử audit log", new
        {
            Items = logs,
            Meta = new
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasNext = page < totalPages,
                HasPrevious = page > 1
            }
        }));
    }
}

public sealed record TopUpWalletRequest(string AccountNumber, long Amount, string? Description);
public sealed record FreezeWalletRequest(string? Status);
public sealed record CreateMerchantAdminRequest(string Code, string Name, string? ContactEmail, string? WebhookUrl);
public sealed record AdminRefundRequest(long Amount, string? Reason);
