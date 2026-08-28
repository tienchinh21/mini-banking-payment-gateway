using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.SharedKernel;
using MiniBanking.SharedKernel.Contracts;

namespace MiniBanking.Modules.Accounts.Application.TopUpWallet;

public sealed class TopUpWalletHandler : IRequestHandler<TopUpWalletCommand, TopUpWalletResponse>
{
    private readonly MiniBankingDbContext _dbContext;
    private readonly IAccountLockService _accountLockService;
    private readonly ILedgerPostingService _ledgerPostingService;

    public TopUpWalletHandler(
        MiniBankingDbContext dbContext,
        IAccountLockService accountLockService,
        ILedgerPostingService ledgerPostingService)
    {
        _dbContext = dbContext;
        _accountLockService = accountLockService;
        _ledgerPostingService = ledgerPostingService;
    }

    public async Task<TopUpWalletResponse> Handle(TopUpWalletCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (request.Amount <= 0)
            throw new ArgumentException("Số tiền nạp phải lớn hơn 0.");

        var wallet = await _dbContext.WalletAccounts
            .FirstOrDefaultAsync(w => w.AccountNumber == request.AccountNumber, cancellationToken);

        if (wallet is null)
            throw new InvalidOperationException("Không tìm thấy ví tài khoản.");

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? wallet.Currency : request.Currency;
        var amount = new Money(request.Amount, currency);

        // 1. Lock and Credit Wallet via AccountLockService (with row-level lock)
        var creditResult = await _accountLockService.LockAndCreditWalletAsync(wallet.Id, amount, cancellationToken);
        if (!creditResult.IsSuccess)
        {
            throw new InvalidOperationException(creditResult.ErrorMessage ?? "Nạp tiền vào ví thất bại.");
        }

        // 2. Post Double-Entry Ledger Transaction
        var description = request.Description ?? $"Top-up wallet {wallet.AccountNumber}";
        var ledgerTx = await _ledgerPostingService.PostTopUpAsync(wallet.Id, amount, description, cancellationToken);

        var snapshot = creditResult.Balance;

        return new TopUpWalletResponse(
            wallet.AccountNumber,
            amount.Amount,
            wallet.Currency,
            snapshot?.AvailableBalance ?? 0,
            snapshot?.LedgerBalance ?? 0,
            ledgerTx.Id);
    }
}
