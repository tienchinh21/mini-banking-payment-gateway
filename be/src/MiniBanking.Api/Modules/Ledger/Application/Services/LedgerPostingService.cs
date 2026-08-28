using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.SharedKernel;
using MiniBanking.SharedKernel.Contracts;

namespace MiniBanking.Modules.Ledger.Application.Services;

public class LedgerPostingService : ILedgerPostingService
{
    private readonly MiniBankingDbContext _dbContext;

    public LedgerPostingService(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<LedgerTransaction> PostPaymentAsync(
        Guid walletAccountId,
        Money amount,
        string description,
        CancellationToken cancellationToken = default)
    {
        var tx = new LedgerTransaction(
            $"PAY-{Guid.NewGuid():N}",
            LedgerTransactionType.Payment,
            description);

        tx.AddEntry(walletAccountId, "WalletAccount", amount, isDebit: true);
        tx.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", amount, isDebit: false);
        tx.ValidateInvariant();

        _dbContext.LedgerTransactions.Add(tx);
        return Task.FromResult(tx);
    }

    public Task<LedgerTransaction> PostRefundAsync(
        Guid walletAccountId,
        Money amount,
        string description,
        CancellationToken cancellationToken = default)
    {
        var tx = new LedgerTransaction(
            $"REF-{Guid.NewGuid():N}",
            LedgerTransactionType.Refund,
            description);

        tx.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", amount, isDebit: true);
        tx.AddEntry(walletAccountId, "WalletAccount", amount, isDebit: false);
        tx.ValidateInvariant();

        _dbContext.LedgerTransactions.Add(tx);
        return Task.FromResult(tx);
    }

    public Task<LedgerTransaction> PostSettlementAsync(
        string merchantId,
        Money netAmount,
        Money feeAmount,
        string description,
        CancellationToken cancellationToken = default)
    {
        var merchantGuid = Guid.TryParse(merchantId, out var g) ? g : Guid.NewGuid();
        var totalAmount = new Money(netAmount.Amount + feeAmount.Amount, netAmount.Currency);

        var tx = new LedgerTransaction(
            $"SET-{Guid.NewGuid():N}",
            LedgerTransactionType.Settlement,
            description);

        tx.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", totalAmount, isDebit: true);
        tx.AddEntry(merchantGuid, "MerchantAccount", netAmount, isDebit: false);
        if (feeAmount.IsPositive)
        {
            tx.AddEntry(SystemAccountIds.PlatformFee, "PlatformFee", feeAmount, isDebit: false);
        }
        tx.ValidateInvariant();

        _dbContext.LedgerTransactions.Add(tx);
        return Task.FromResult(tx);
    }

    public Task<LedgerTransaction> PostTopUpAsync(
        Guid walletAccountId,
        Money amount,
        string description,
        CancellationToken cancellationToken = default)
    {
        var tx = new LedgerTransaction(
            $"TOPUP-{Guid.NewGuid():N}",
            LedgerTransactionType.TopUp,
            description);

        tx.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", amount, isDebit: true);
        tx.AddEntry(walletAccountId, "WalletAccount", amount, isDebit: false);
        tx.ValidateInvariant();

        _dbContext.LedgerTransactions.Add(tx);
        return Task.FromResult(tx);
    }
}
