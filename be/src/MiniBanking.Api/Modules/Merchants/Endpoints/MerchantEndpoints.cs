using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniBanking.Modules.Merchants.Application.Commands.CreateMerchant;
using MiniBanking.Modules.Merchants.Application.Commands.DeactivateMerchant;
using MiniBanking.Modules.Merchants.Application.Commands.RegenerateMerchantKeys;
using MiniBanking.Modules.Merchants.Application.Commands.UpdateMerchant;
using MiniBanking.Modules.Merchants.Application.Queries.GetMerchantById;
using MiniBanking.Modules.Merchants.Application.Queries.GetMerchants;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Merchants.Endpoints;

public sealed record CreateMerchantAdminRequest(
    string MerchantId,
    string Name,
    string? WebhookUrl);

public sealed record UpdateMerchantAdminRequest(
    string Name,
    string? WebhookUrl,
    bool IsActive);

public static class MerchantEndpoints
{
    public static IEndpointRouteBuilder MapMerchantEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/admin/merchants")
            .RequireAuthorization("Admin")
            .WithTags("Admin - Merchants");

        group.MapGet("/", GetMerchants);
        group.MapGet("/{id}", GetMerchantById);
        group.MapPost("/", CreateMerchant);
        group.MapPut("/{id}", UpdateMerchant);
        group.MapDelete("/{id}", DeactivateMerchant);
        group.MapPost("/{id}/regenerate-keys", RegenerateMerchantKeys);

        return routes;
    }

    private static async Task<IResult> GetMerchants(
        int? page,
        int? pageSize,
        string? search,
        string? keyword,
        bool? isActive,
        string? status,
        IMediator mediator,
        CancellationToken ct)
    {
        bool? activeFilter = isActive;
        if (!activeFilter.HasValue && !string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                activeFilter = true;
            else if (string.Equals(status, "SUSPENDED", StringComparison.OrdinalIgnoreCase))
                activeFilter = false;
        }

        var searchTerm = !string.IsNullOrWhiteSpace(search) ? search : keyword;
        var query = new GetMerchantsQuery(page, pageSize, searchTerm, activeFilter);
        var result = await mediator.Send(query, ct);

        return Results.Ok(ApiResponse.Ok("Danh sách đối tác tích hợp", result));
    }

    private static async Task<IResult> GetMerchantById(
        string id,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new GetMerchantByIdQuery(id);
        var result = await mediator.Send(query, ct);

        if (result is null)
            return Results.NotFound(ApiResponse.Fail("Không tìm thấy đối tác."));

        return Results.Ok(ApiResponse.Ok("Chi tiết đối tác", result));
    }

    private static async Task<IResult> CreateMerchant(
        CreateMerchantAdminRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var command = new CreateMerchantCommand(request.MerchantId, request.Name, request.WebhookUrl);
            var result = await mediator.Send(command, ct);
            return Results.Ok(ApiResponse.Ok("Tạo đối tác thành công", result));
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(ApiResponse.Fail(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    private static async Task<IResult> UpdateMerchant(
        string id,
        UpdateMerchantAdminRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var command = new UpdateMerchantCommand(id, request.Name, request.WebhookUrl, request.IsActive);
            var result = await mediator.Send(command, ct);
            return Results.Ok(ApiResponse.Ok("Cập nhật đối tác thành công", result));
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(ApiResponse.Fail(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    private static async Task<IResult> DeactivateMerchant(
        string id,
        IMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var command = new DeactivateMerchantCommand(id);
            var result = await mediator.Send(command, ct);
            return Results.Ok(ApiResponse.Ok("Vô hiệu hóa đối tác thành công", result));
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(ApiResponse.Fail(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    private static async Task<IResult> RegenerateMerchantKeys(
        string id,
        IMediator mediator,
        CancellationToken ct)
    {
        try
        {
            var command = new RegenerateMerchantKeysCommand(id);
            var result = await mediator.Send(command, ct);
            return Results.Ok(ApiResponse.Ok("Cấp lại khóa bảo mật thành công", result));
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(ApiResponse.Fail(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse.Fail(ex.Message));
        }
    }
}
