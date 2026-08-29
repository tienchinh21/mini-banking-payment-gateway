using MediatR;
using MiniBanking.SharedKernel.Behaviors;

namespace MiniBanking.Modules.Merchants.Application.Commands.RegenerateMerchantKeys;

public sealed record RegenerateMerchantKeysCommand(string Id) : IRequest<RegenerateMerchantKeysResponse>, ITransactionalRequest;

public sealed record RegenerateMerchantKeysResponse(
    Guid Id,
    string MerchantId,
    string Code,
    string ApiKey,
    string Secret,
    DateTime? UpdatedAt
);
