using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniBanking.Modules.Ledger.Application.Commands.ReconcileLedger;
using MiniBanking.Modules.Ledger.Application.Queries.GetLedgerEntries;
using MiniBanking.Modules.Ledger.Application.Queries.GetLedgerTransactionById;
using MiniBanking.Modules.Ledger.Application.Queries.GetLedgerTransactions;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Ledger.Endpoints;

public static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder routes)
    {
        // 1. Public Ledger Endpoints
        var publicGroup = routes.MapGroup("/api/v1/ledger").WithTags("Ledger");

        publicGroup.MapGet("/transactions/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var query = new GetLedgerTransactionByIdQuery(id);
            var result = await mediator.Send(query);

            if (result is null)
                return Results.NotFound(ApiResponse.Fail("Không tìm thấy giao dịch sổ cái."));

            return Results.Ok(ApiResponse.Ok("Chi tiết giao dịch sổ cái", result));
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
            IMediator mediator) =>
        {
            var query = new GetLedgerEntriesQuery(
                page,
                pageSize,
                accountId,
                currency,
                isDebit,
                fromDate,
                toDate);

            var result = await mediator.Send(query);
            return Results.Ok(ApiResponse.Ok("Danh sách bút toán sổ cái", result));
        });

        adminGroup.MapGet("/transactions", async (
            int? page,
            int? pageSize,
            string? type,
            IMediator mediator) =>
        {
            var query = new GetLedgerTransactionsQuery(page, pageSize, type);
            var result = await mediator.Send(query);
            return Results.Ok(ApiResponse.Ok("Danh sách giao dịch sổ cái", result));
        });

        adminGroup.MapPost("/reconcile", async (IMediator mediator) =>
        {
            var command = new ReconcileLedgerCommand();
            var result = await mediator.Send(command);
            return Results.Ok(ApiResponse.Ok("Kết quả đối soát số dư", result));
        });

        return routes;
    }
}
