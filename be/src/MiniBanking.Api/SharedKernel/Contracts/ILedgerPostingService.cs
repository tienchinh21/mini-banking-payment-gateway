using MiniBanking.Modules.Ledger.Domain;

namespace MiniBanking.SharedKernel.Contracts;

public interface ILedgerPostingService
{
    /// <summary>
    /// Posts a balanced direct debit payment transaction (Debit Wallet, Credit Clearing).
    /// </summary>
    Task<LedgerTransaction> PostPaymentAsync(
        Guid walletAccountId,
        Money amount,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a balanced refund transaction (Debit Clearing, Credit Wallet).
    /// </summary>
    Task<LedgerTransaction> PostRefundAsync(
        Guid walletAccountId,
        Money amount,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a balanced merchant settlement transaction with net payout and platform fee.
    /// </summary>
    Task<LedgerTransaction> PostSettlementAsync(
        string merchantId,
        Money netAmount,
        Money feeAmount,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts a balanced wallet top-up transaction (Debit Clearing, Credit Wallet).
    /// </summary>
    Task<LedgerTransaction> PostTopUpAsync(
        Guid walletAccountId,
        Money amount,
        string description,
        CancellationToken cancellationToken = default);
}
