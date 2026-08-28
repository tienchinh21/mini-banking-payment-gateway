using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Merchants.Domain;
using MiniBanking.SharedKernel;
using System.Globalization;
using System.Text;

namespace MiniBanking.Infrastructure.Security;

public class HmacVerificationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TimeSpan _timestampTolerance = TimeSpan.FromMinutes(5);

    public HmacVerificationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, MiniBankingDbContext dbContext, IMemoryCache memoryCache)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (!path.StartsWith("/api/v1/merchant", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var merchantId = context.Request.Headers["X-Merchant-Id"].FirstOrDefault();
        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        var timestamp = context.Request.Headers["X-Timestamp"].FirstOrDefault();
        var nonce = context.Request.Headers["X-Nonce"].FirstOrDefault();
        var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        var signature = context.Request.Headers["X-Signature"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(merchantId) ||
            string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(timestamp) ||
            string.IsNullOrWhiteSpace(nonce) ||
            string.IsNullOrWhiteSpace(idempotencyKey) ||
            string.IsNullOrWhiteSpace(signature))
        {
            await WriteUnauthorizedResponse(context, "Thiếu header bảo mật bắt buộc.");
            return;
        }

        if (!DateTime.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var requestTime) ||
            Math.Abs((DateTime.UtcNow - requestTime).TotalMinutes) > _timestampTolerance.TotalMinutes)
        {
            await WriteUnauthorizedResponse(context, "Timestamp không hợp lệ hoặc đã hết hạn.");
            return;
        }

        var nonceCacheKey = $"nonce:{merchantId}:{nonce}";
        if (memoryCache.TryGetValue(nonceCacheKey, out _))
        {
            await WriteUnauthorizedResponse(context, "Nonce đã được sử dụng (Replay attack detected).");
            return;
        }

        var merchant = await dbContext.Merchants
            .FirstOrDefaultAsync(m => m.MerchantId == merchantId && m.ApiKey == apiKey && m.IsActive);

        if (merchant is null)
        {
            await WriteUnauthorizedResponse(context, "Merchant không tồn tại hoặc API key không hợp lệ.");
            return;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var isValid = HmacSignatureService.VerifySignature(
            context.Request.Method,
            path,
            body,
            timestamp,
            nonce,
            idempotencyKey,
            signature,
            merchant.Secret);

        if (!isValid)
        {
            await WriteUnauthorizedResponse(context, "Chữ ký HMAC không hợp lệ.");
            return;
        }

        // Cache the nonce for the duration of the timestamp tolerance window
        memoryCache.Set(nonceCacheKey, true, _timestampTolerance);

        context.Items["MerchantId"] = merchant.MerchantId;
        context.Items["IdempotencyKey"] = idempotencyKey;

        await _next(context);
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ApiResponse.Fail(message));
    }
}
