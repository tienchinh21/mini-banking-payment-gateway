using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniBanking.Modules.Payments.Application.CreatePayment;
using MiniBanking.Modules.Payments.Application.CreateRefund;
using MiniBanking.Modules.Payments.Application.CreateSettlement;
using MiniBanking.SharedKernel;
using System.Text;

namespace MiniBanking.Modules.Payments.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder routes)
    {
        var merchantGroup = routes.MapGroup("/api/v1/merchant").WithTags("Merchant Payments");

        merchantGroup.MapPost("/payments", CreatePayment);
        merchantGroup.MapPost("/refunds", CreateRefund);
        merchantGroup.MapPost("/settlements", CreateSettlement);

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
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ApiResponse.Fail(ex.Message));
        }
    }
}
