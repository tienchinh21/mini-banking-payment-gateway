using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Payments.Application.CreatePayment;
using MiniBanking.Modules.Payments.Application.CreateRefund;
using MiniBanking.Modules.Payments.Application.CreateSettlement;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;
using System.Text;

namespace MiniBanking.Modules.Payments.Endpoints;

public sealed record AdminRefundRequest(long? Amount, string? Reason);

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder routes)
    {
        // 1. Merchant Public Payment APIs (Authenticated by HMAC Middleware)
        var merchantGroup = routes.MapGroup("/api/v1/merchant").WithTags("Merchant Payments");

        merchantGroup.MapPost("/payments", CreatePayment);
        merchantGroup.MapPost("/refunds", CreateRefund);
        merchantGroup.MapPost("/settlements", CreateSettlement);

        // 2. Admin Payment & Settlement Operations
        var adminPaymentGroup = routes.MapGroup("/api/v1/admin/payments")
            .RequireAuthorization("Admin")
            .WithTags("Admin - Payments");

        adminPaymentGroup.MapGet("/", async (
            int? page,
            int? pageSize,
            string? status,
            string? merchantId,
            DateTime? fromDate,
            DateTime? toDate,
            MiniBankingDbContext db) =>
        {
            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.Payments.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var st))
                query = query.Where(x => x.Status == st);

            if (!string.IsNullOrWhiteSpace(merchantId))
                query = query.Where(x => x.MerchantId == merchantId);

            if (fromDate.HasValue)
                query = query.Where(x => x.CreatedAt >= fromDate.Value.ToUniversalTime());

            if (toDate.HasValue)
                query = query.Where(x => x.CreatedAt <= toDate.Value.ToUniversalTime());

            var total = await query.CountAsync();
            var payments = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();

            var walletIds = payments.Select(x => x.WalletAccountId).Distinct().ToList();
            var wallets = await db.WalletAccounts
                .Include(w => w.Customer)
                .AsNoTracking()
                .Where(w => walletIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id);

            var items = payments.Select(x =>
            {
                wallets.TryGetValue(x.WalletAccountId, out var w);
                return new
                {
                    PaymentId = x.Id,
                    x.MerchantId,
                    x.MerchantOrderId,
                    x.WalletAccountId,
                    WalletAccountNumber = w?.AccountNumber,
                    CustomerName = w?.Customer.FullName,
                    CustomerEmail = w?.Customer.Email,
                    x.Amount,
                    x.Currency,
                    Status = x.Status.ToString(),
                    x.FailureCode,
                    x.Description,
                    x.IdempotencyKey,
                    x.LedgerTransactionId,
                    x.CreatedAt,
                    x.UpdatedAt
                };
            });

            return Results.Ok(ApiResponse.Ok("Danh sách giao dịch thanh toán", new
            {
                Items = items,
                TotalCount = total,
                Page = p,
                PageSize = ps,
                TotalPages = (int)Math.Ceiling(total / (double)ps)
            }));
        });

        adminPaymentGroup.MapGet("/{id:guid}", async (Guid id, MiniBankingDbContext db) =>
        {
            var payment = await db.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (payment is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy giao dịch thanh toán."));

            var wallet = await db.WalletAccounts
                .Include(w => w.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == payment.WalletAccountId);

            var refunds = await db.Refunds
                .AsNoTracking()
                .Where(r => r.PaymentId == payment.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Results.Ok(ApiResponse.Ok("Chi tiết giao dịch thanh toán", new
            {
                PaymentId = payment.Id,
                payment.MerchantId,
                payment.MerchantOrderId,
                payment.WalletAccountId,
                WalletAccountNumber = wallet?.AccountNumber,
                CustomerName = wallet?.Customer.FullName,
                CustomerEmail = wallet?.Customer.Email,
                payment.Amount,
                payment.Currency,
                Status = payment.Status.ToString(),
                payment.FailureCode,
                payment.Description,
                payment.IdempotencyKey,
                payment.LedgerTransactionId,
                payment.CreatedAt,
                payment.UpdatedAt,
                Refunds = refunds.Select(r => new
                {
                    RefundId = r.Id,
                    r.MerchantRefundId,
                    r.Amount,
                    r.Currency,
                    Status = r.Status.ToString(),
                    r.Reason,
                    r.CreatedAt
                })
            }));
        });

        adminPaymentGroup.MapPost("/{id}/refund", async (string id, AdminRefundRequest request, IMediator mediator, MiniBankingDbContext db) =>
        {
            if (!Guid.TryParse(id, out var paymentGuid))
                return Results.BadRequest(ApiResponse.Fail("Payment ID không hợp lệ."));

            var payment = await db.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == paymentGuid);

            if (payment is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy giao dịch."));

            var refundAmount = request.Amount ?? payment.Amount;
            var refundReq = new CreateRefundRequest(
                $"ADM-REF-{Guid.NewGuid():N}"[..20],
                payment.Id,
                refundAmount,
                payment.Currency,
                request.Reason ?? "Admin manual refund");

            var command = new CreateRefundCommand(
                payment.MerchantId,
                $"adm-idemp-{Guid.NewGuid():N}",
                "POST",
                $"/api/v1/admin/payments/{id}/refund",
                System.Text.Json.JsonSerializer.Serialize(refundReq),
                refundReq);

            try
            {
                var result = await mediator.Send(command);
                return result.Status == "Succeeded"
                    ? Results.Ok(ApiResponse.Ok("Hoàn tiền thành công", result))
                    : Results.BadRequest(ApiResponse.Fail($"Hoàn tiền thất bại: {result.FailureCode}"));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ApiResponse.Fail($"Lỗi hoàn tiền: {ex.Message}"));
            }
        });

        // 3. Admin Settlements
        var adminSettlementGroup = routes.MapGroup("/api/v1/admin/settlements")
            .RequireAuthorization("Admin")
            .WithTags("Admin - Settlements");

        adminSettlementGroup.MapPost("/", async (CreateSettlementRequest request, IMediator mediator) =>
        {
            try
            {
                var command = new CreateSettlementCommand(request);
                var result = await mediator.Send(command);
                return Results.Ok(ApiResponse.Ok("Quyết toán thành công", result));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ApiResponse.Fail(ex.Message));
            }
        });

        return routes;
    }

    private static async Task<IResult> CreatePayment(
        CreatePaymentRequest request,
        HttpContext context,
        IMediator mediator)
    {
        var merchantId = context.Items["MerchantId"] as string;
        var idempotencyKey = context.Items["IdempotencyKey"] as string;

        if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(idempotencyKey))
            return Results.Unauthorized();

        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var command = new CreatePaymentCommand(
            merchantId,
            idempotencyKey,
            context.Request.Method,
            context.Request.Path.Value ?? "/api/v1/merchant/payments",
            body,
            request);

        try
        {
            var response = await mediator.Send(command);
            return response.Status == "Succeeded"
                ? Results.Ok(ApiResponse.Ok("Thanh toán thành công", response))
                : Results.Ok(ApiResponse.Ok("Thanh toán thất bại", response));
        }
        catch (FluentValidation.ValidationException ex)
        {
            return Results.BadRequest(ApiResponse.Fail(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ApiResponse.Fail(ex.Message));
        }
    }

    private static async Task<IResult> CreateRefund(
        CreateRefundRequest request,
        HttpContext context,
        IMediator mediator)
    {
        var merchantId = context.Items["MerchantId"] as string;
        var idempotencyKey = context.Items["IdempotencyKey"] as string;

        if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(idempotencyKey))
            return Results.Unauthorized();

        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var command = new CreateRefundCommand(
            merchantId,
            idempotencyKey,
            context.Request.Method,
            context.Request.Path.Value ?? "/api/v1/merchant/refunds",
            body,
            request);

        try
        {
            var response = await mediator.Send(command);
            return response.Status == "Succeeded"
                ? Results.Ok(ApiResponse.Ok("Hoàn tiền thành công", response))
                : Results.Ok(ApiResponse.Ok("Hoàn tiền thất bại", response));
        }
        catch (FluentValidation.ValidationException ex)
        {
            return Results.BadRequest(ApiResponse.Fail(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ApiResponse.Fail(ex.Message));
        }
    }

    private static async Task<IResult> CreateSettlement(
        CreateSettlementRequest request,
        IMediator mediator)
    {
        try
        {
            var command = new CreateSettlementCommand(request);
            var response = await mediator.Send(command);
            return Results.Ok(ApiResponse.Ok("Quyết toán thành công", response));
        }
        catch (FluentValidation.ValidationException ex)
        {
            return Results.BadRequest(ApiResponse.Fail(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ApiResponse.Fail(ex.Message));
        }
    }
}
