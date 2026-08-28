using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Merchants.Domain;
using MiniBanking.SharedKernel;
using System.Security.Cryptography;

namespace MiniBanking.Modules.Merchants.Endpoints;

public sealed record CreateMerchantAdminRequest(
    string MerchantId,
    string Name,
    string? WebhookUrl);

public static class MerchantEndpoints
{
    public static IEndpointRouteBuilder MapMerchantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/admin/merchants")
            .RequireAuthorization("Admin")
            .WithTags("Admin - Merchants");

        group.MapGet("/", async (
            int? page,
            int? pageSize,
            string? search,
            bool? isActive,
            MiniBankingDbContext db) =>
        {
            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.Merchants.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(m =>
                    m.MerchantId.ToLower().Contains(s) ||
                    m.Name.ToLower().Contains(s));
            }

            if (isActive.HasValue)
                query = query.Where(m => m.IsActive == isActive.Value);

            var total = await query.CountAsync();
            var merchants = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();

            return Results.Ok(ApiResponse.Ok("Danh sách đối tác tích hợp", new
            {
                Items = merchants.Select(m => new
                {
                    m.Id,
                    m.MerchantId,
                    m.Name,
                    m.ApiKey,
                    SecretMasked = MaskSecret(m.Secret),
                    m.WebhookUrl,
                    m.IsActive,
                    m.CreatedAt,
                    m.UpdatedAt
                }),
                TotalCount = total,
                Page = p,
                PageSize = ps,
                TotalPages = (int)Math.Ceiling(total / (double)ps)
            }));
        });

        group.MapGet("/{id}", async (string id, MiniBankingDbContext db) =>
        {
            Merchant? merchant;
            if (Guid.TryParse(id, out var g))
                merchant = await db.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.Id == g);
            else
                merchant = await db.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.MerchantId == id);

            if (merchant is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy đối tác."));

            var paymentsCount = await db.Payments
                .AsNoTracking()
                .CountAsync(p => p.MerchantId == merchant.MerchantId);

            var totalPaid = await db.Payments
                .AsNoTracking()
                .Where(p => p.MerchantId == merchant.MerchantId && p.Status == Modules.Payments.Domain.PaymentStatus.Succeeded)
                .SumAsync(p => p.Amount);

            return Results.Ok(ApiResponse.Ok("Chi tiết đối tác", new
            {
                merchant.Id,
                merchant.MerchantId,
                merchant.Name,
                merchant.ApiKey,
                merchant.Secret,
                merchant.WebhookUrl,
                merchant.IsActive,
                merchant.CreatedAt,
                merchant.UpdatedAt,
                TotalPayments = paymentsCount,
                TotalVolume = totalPaid
            }));
        });

        group.MapPost("/", async (CreateMerchantAdminRequest request, MiniBankingDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.MerchantId) || string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(ApiResponse.Fail("MerchantId và Name không được để trống."));

            var exists = await db.Merchants
                .AsNoTracking()
                .AnyAsync(m => m.MerchantId == request.MerchantId);

            if (exists)
                return Results.Conflict(ApiResponse.Fail("MerchantId đã tồn tại trong hệ thống."));

            var apiKey = $"mb_live_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}";
            var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

            var merchant = new Merchant(
                request.MerchantId,
                request.Name,
                apiKey,
                secret,
                request.WebhookUrl);

            db.Merchants.Add(merchant);
            await db.SaveChangesAsync();

            return Results.Ok(ApiResponse.Ok("Tạo đối tác thành công", new
            {
                merchant.Id,
                merchant.MerchantId,
                merchant.Name,
                merchant.ApiKey,
                merchant.Secret,
                merchant.WebhookUrl,
                merchant.IsActive,
                merchant.CreatedAt
            }));
        });

        group.MapPost("/{id}/regenerate-keys", async (string id, MiniBankingDbContext db) =>
        {
            Merchant? merchant;
            if (Guid.TryParse(id, out var g))
                merchant = await db.Merchants.FirstOrDefaultAsync(m => m.Id == g);
            else
                merchant = await db.Merchants.FirstOrDefaultAsync(m => m.MerchantId == id);

            if (merchant is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy đối tác."));

            var newApiKey = $"mb_live_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}";
            var newSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

            merchant.RegenerateCredentials(newApiKey, newSecret);
            await db.SaveChangesAsync();

            return Results.Ok(ApiResponse.Ok("Cấp lại khóa bảo mật thành công", new
            {
                merchant.MerchantId,
                merchant.ApiKey,
                merchant.Secret,
                merchant.UpdatedAt
            }));
        });

        return routes;
    }

    private static string MaskSecret(string secret)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length < 8) return "••••••••";
        return $"{secret[..4]}••••••••{secret[^4..]}";
    }
}
