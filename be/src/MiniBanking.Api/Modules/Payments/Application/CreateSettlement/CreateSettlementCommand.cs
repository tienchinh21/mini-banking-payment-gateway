using MediatR;
using MiniBanking.SharedKernel.Behaviors;

namespace MiniBanking.Modules.Payments.Application.CreateSettlement;

public sealed class CreateSettlementCommand : IRequest<CreateSettlementResponse>, ITransactionalRequest
{
    public CreateSettlementRequest Request { get; }

    public CreateSettlementCommand(CreateSettlementRequest request)
    {
        Request = request;
    }
}
