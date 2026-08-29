using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniBanking.Modules.Admin.Application;
using MiniBanking.Modules.Admin.Application.Commands.AdminLogin;
using MiniBanking.Modules.Admin.Application.Queries.GetAdminProfile;
using MiniBanking.Modules.Admin.Application.Queries.GetDashboardStats;
using MiniBanking.Modules.Admin.Application.Queries.GetMerchantSettlementSummary;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Admin.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/admin");

        // 1. Admin Authentication Endpoints
        group.MapPost("/auth/login", async (LoginRequest request, IMediator mediator) =>
        {
            var command = new AdminLoginCommand(request.Email, request.Password);
            var result = await mediator.Send(command);

            if (result is null)
                return Results.Unauthorized();

            return Results.Ok(ApiResponse.Ok("Đăng nhập thành công", result));
        });

        group.MapPost("/auth/logout", () =>
        {
            return Results.Ok(ApiResponse.Ok("Đăng xuất thành công"));
        }).RequireAuthorization("Admin");

        group.MapGet("/auth/profile", async (ClaimsPrincipal user, IMediator mediator) =>
        {
            var profile = await mediator.Send(new GetAdminProfileQuery(user));
            return Results.Ok(ApiResponse.Ok("Thông tin tài khoản", profile));
        }).RequireAuthorization("Admin");

        // 2. Admin Dashboard Statistics
        group.MapGet("/dashboard/stats", async (IMediator mediator) =>
        {
            var stats = await mediator.Send(new GetDashboardStatsQuery());
            return Results.Ok(ApiResponse.Ok("Thống kê tổng quan", stats));
        }).RequireAuthorization("Admin");

        // 3. Admin Merchant Settlement Summary
        group.MapGet("/merchants/settlement-summary", async (string? merchantId, IMediator mediator) =>
        {
            var summary = await mediator.Send(new GetMerchantSettlementSummaryQuery(merchantId));
            return Results.Ok(ApiResponse.Ok("Tổng hợp quyết toán đối tác", summary));
        }).RequireAuthorization("Admin");

        return routes;
    }
}
