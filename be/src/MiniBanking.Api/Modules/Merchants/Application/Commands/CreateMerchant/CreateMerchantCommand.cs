using MediatR;
using MiniBanking.SharedKernel.Behaviors;

namespace MiniBanking.Modules.Merchants.Application.Commands.CreateMerchant;

public sealed record CreateMerchantCommand(
    string MerchantId,
    string Name,
    string? WebhookUrl = null
) : IRequest<CreateMerchantResponse>, ITransactionalRequest;

public sealed record CreateMerchantResponse(
    Guid Id,
    string MerchantId,
    string Code,
    string Name,
    string ApiKey,
    string Secret,
    string? WebhookUrl,
    bool IsActive,
    string Status,
    DateTime CreatedAt
);
