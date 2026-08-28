using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Admin.Application;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Admin.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/admin");

        // 1. Admin Authentication Endpoints
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

            return Results.Ok(ApiResponse.Ok("Đăng nhập thành công", new
            {
                Token = token,
                User = new
                {
                    admin.Id,
                    admin.Email,
                    admin.FullName,
                    admin.Role
                }
            }));
        });

        group.MapPost("/auth/logout", () =>
        {
            return Results.Ok(ApiResponse.Ok("Đăng xuất thành công"));
        }).RequireAuthorization("Admin");

        group.MapGet("/auth/profile", (HttpContext context) =>
        {
            var user = context.User;
            return Results.Ok(ApiResponse.Ok("Thông tin tài khoản", new
            {
                Id = user.FindFirstValue(ClaimTypes.NameIdentifier),
                Email = user.FindFirstValue(ClaimTypes.Email),
                FullName = user.FindFirstValue(ClaimTypes.Name),
                Role = user.FindFirstValue(ClaimTypes.Role)
            }));
        }).RequireAuthorization("Admin");

        // 2. Admin Dashboard Statistics
        group.MapGet("/dashboard/stats", async (MiniBankingDbContext db) =>
        {
            var totalWallets = await db.WalletAccounts.CountAsync();
            var totalCustomers = await db.BankingCustomers.CountAsync();
            var totalMerchants = await db.Merchants.CountAsync(m => m.IsActive);

            var totalPayments = await db.Payments.CountAsync();
            var successfulPayments = await db.Payments.CountAsync(p => p.Status == PaymentStatus.Succeeded);
            var failedPayments = await db.Payments.CountAsync(p => p.Status == PaymentStatus.Failed);

            var totalVolume = await db.Payments
                .Where(p => p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => p.Amount);

            var totalRefunds = await db.Refunds.CountAsync(r => r.Status == RefundStatus.Succeeded);
            var totalRefundAmount = await db.Refunds
                .Where(r => r.Status == RefundStatus.Succeeded)
                .SumAsync(r => r.Amount);

            var today = DateTime.UtcNow.Date;
            var todayPayments = await db.Payments
                .Where(p => p.CreatedAt >= today && p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => p.Amount);
            var todayTxCount = await db.Payments
                .CountAsync(p => p.CreatedAt >= today);

            var recentPayments = await db.Payments
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new
                {
                    p.Id,
                    p.MerchantId,
                    p.MerchantOrderId,
                    p.Amount,
                    p.Currency,
                    Status = p.Status.ToString(),
                    p.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(ApiResponse.Ok("Thống kê tổng quan", new
            {
                Wallets = new { Total = totalWallets, Customers = totalCustomers },
                Merchants = new { Total = totalMerchants },
                Payments = new
                {
                    Total = totalPayments,
                    Successful = successfulPayments,
                    Failed = failedPayments,
                    TotalVolume = totalVolume,
                    TodayVolume = todayPayments,
                    TodayCount = todayTxCount
                },
                Refunds = new { TotalCount = totalRefunds, TotalAmount = totalRefundAmount },
                RecentPayments = recentPayments
            }));
        }).RequireAuthorization("Admin");

        return routes;
    }
}
