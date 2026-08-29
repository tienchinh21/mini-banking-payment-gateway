using System.Text;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniBanking.Modules.Payments.Application.Commands.AdminRefund;
using MiniBanking.Modules.Payments.Application.CreatePayment;
using MiniBanking.Modules.Payments.Application.CreateRefund;
using MiniBanking.Modules.Payments.Application.CreateSettlement;
using MiniBanking.Modules.Payments.Application.Queries.GetPaymentById;
using MiniBanking.Modules.Payments.Application.Queries.GetPayments;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Payments.Endpoints;

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
            IMediator mediator) =>
        {
            var query = new GetPaymentsQuery(page, pageSize, status, merchantId, fromDate, toDate);
            var result = await mediator.Send(query);

            return Results.Ok(ApiResponse.Ok("Danh sách giao dịch thanh toán", result));
        });

        adminPaymentGroup.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPaymentByIdQuery(id));

            if (result is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy giao dịch thanh toán."));

            return Results.Ok(ApiResponse.Ok("Chi tiết giao dịch thanh toán", result));
        });

        adminPaymentGroup.MapPost("/{id}/refund", async (string id, AdminRefundRequest? request, IMediator mediator) =>
        {
            if (!Guid.TryParse(id, out var paymentGuid))
                return Results.BadRequest(ApiResponse.Fail("Payment ID không hợp lệ."));

            try
            {
                var command = new AdminRefundCommand(paymentGuid, request?.Amount, request?.Reason);
                var result = await mediator.Send(command);

                return result.Status == "Succeeded"
                    ? Results.Ok(ApiResponse.Ok("Hoàn tiền thành công", result))
                    : Results.BadRequest(ApiResponse.Fail($"Hoàn tiền thất bại: {result.FailureCode}"));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(ApiResponse.Fail(ex.Message));
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
            catch (FluentValidation.ValidationException ex)
            {
                return Results.BadRequest(ApiResponse.Fail(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))));
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
