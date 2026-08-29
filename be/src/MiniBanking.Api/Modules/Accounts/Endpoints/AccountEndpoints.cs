using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniBanking.Modules.Accounts.Application.Commands.FreezeWallet;
using MiniBanking.Modules.Accounts.Application.Queries.GetWalletBalance;
using MiniBanking.Modules.Accounts.Application.Queries.GetWalletBalanceByNumber;
using MiniBanking.Modules.Accounts.Application.Queries.GetWalletDetail;
using MiniBanking.Modules.Accounts.Application.Queries.GetWalletLedger;
using MiniBanking.Modules.Accounts.Application.Queries.GetWallets;
using MiniBanking.Modules.Accounts.Application.TopUpWallet;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Accounts.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        // Public Account Endpoints
        var accountGroup = routes.MapGroup("/api/v1/accounts").WithTags("Accounts");

        accountGroup.MapGet("/{id:guid}/balance", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetWalletBalanceQuery(id));
            if (result is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy ví tài khoản."));

            return Results.Ok(ApiResponse.Ok("Thông tin số dư ví", result));
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
            IMediator mediator) =>
        {
            var result = await mediator.Send(new GetWalletsQuery(page, pageSize, search, currency));
            return Results.Ok(ApiResponse.Ok("Danh sách ví tài khoản", result));
        });

        adminWalletGroup.MapGet("/{id}", async (string id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetWalletDetailQuery(id));
            if (result is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy ví tài khoản."));

            return Results.Ok(ApiResponse.Ok("Chi tiết ví tài khoản", result));
        });

        adminWalletGroup.MapGet("/{accountNumber}/balance", async (string accountNumber, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetWalletBalanceByNumberQuery(accountNumber));
            if (result is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy ví tài khoản."));

            return Results.Ok(ApiResponse.Ok("Số dư ví", result));
        });

        adminWalletGroup.MapGet("/{accountNumber}/ledger", async (string accountNumber, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetWalletLedgerQuery(accountNumber));
            if (result is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy ví tài khoản."));

            return Results.Ok(ApiResponse.Ok("Lịch sử biến động số dư", result));
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

        adminWalletGroup.MapPost("/{id}/freeze", async (string id, FreezeWalletRequest? req, IMediator mediator) =>
        {
            var command = new FreezeWalletCommand(id, req?.Reason);
            var response = await mediator.Send(command);
            return Results.Ok(ApiResponse.Ok("Khóa ví thành công (mô phỏng)", response));
        });

        return routes;
    }
}
