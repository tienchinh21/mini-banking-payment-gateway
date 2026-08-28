using MediatR;
using MiniBanking.SharedKernel.Behaviors;

namespace MiniBanking.Modules.Accounts.Application.TopUpWallet;

public sealed class TopUpWalletCommand : IRequest<TopUpWalletResponse>, ITransactionalRequest
{
    public TopUpWalletRequest Request { get; }

    public TopUpWalletCommand(TopUpWalletRequest request)
    {
        Request = request;
    }
}
