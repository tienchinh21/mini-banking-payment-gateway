using MediatR;

namespace MiniBanking.Modules.Accounts.Application.Commands.FreezeWallet;

public sealed record FreezeWalletRequest(string? Reason);

public sealed record FreezeWalletResponse(
    string WalletId,
    string Status,
    string Reason,
    DateTime FrozenAt);

public sealed record FreezeWalletCommand(string WalletId, string? Reason) : IRequest<FreezeWalletResponse>;
