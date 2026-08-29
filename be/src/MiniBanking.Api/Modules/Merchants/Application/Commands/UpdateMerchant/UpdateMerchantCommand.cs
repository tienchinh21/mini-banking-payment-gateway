using MediatR;
using MiniBanking.SharedKernel.Behaviors;

namespace MiniBanking.Modules.Merchants.Application.Commands.UpdateMerchant;

public sealed record UpdateMerchantCommand(
    string Id,
    string Name,
    string? WebhookUrl,
    bool IsActive
) : IRequest<UpdateMerchantResponse>, ITransactionalRequest;

public sealed record UpdateMerchantResponse(
    Guid Id,
    string MerchantId,
    string Code,
    string Name,
    string ApiKey,
    string? WebhookUrl,
    bool IsActive,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
