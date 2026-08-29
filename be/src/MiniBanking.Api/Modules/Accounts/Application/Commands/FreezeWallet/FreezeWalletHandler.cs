using MediatR;

namespace MiniBanking.Modules.Accounts.Application.Commands.FreezeWallet;

public sealed class FreezeWalletHandler : IRequestHandler<FreezeWalletCommand, FreezeWalletResponse>
{
    public Task<FreezeWalletResponse> Handle(FreezeWalletCommand command, CancellationToken cancellationToken)
    {
        var response = new FreezeWalletResponse(
            command.WalletId,
            "Frozen",
            command.Reason ?? "Admin action",
            DateTime.UtcNow);

        return Task.FromResult(response);
    }
}
