using MediatR;
using MiniBanking.SharedKernel.Behaviors;

namespace MiniBanking.Modules.Merchants.Application.Commands.DeactivateMerchant;

public sealed record DeactivateMerchantCommand(string Id) : IRequest<DeactivateMerchantResponse>, ITransactionalRequest;

public sealed record DeactivateMerchantResponse(
    Guid Id,
    string MerchantId,
    bool IsActive,
    string Status,
    DateTime? UpdatedAt
);
