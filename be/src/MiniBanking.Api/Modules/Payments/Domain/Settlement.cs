using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Payments.Domain;

public enum SettlementStatus
{
    Pending = 1,
    Completed = 2
}

public class Settlement : Entity
{
    public string MerchantId { get; private set; } = string.Empty;
    public string BatchReference { get; private set; } = string.Empty;
    public long Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public int PaymentCount { get; private set; }
    public SettlementStatus Status { get; private set; } = SettlementStatus.Pending;
    public Guid? LedgerTransactionId { get; private set; }

    private Settlement() { } // EF Core requires parameterless constructor

    public Settlement(
        string merchantId,
        string batchReference,
        Money amount,
        int paymentCount)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));

        if (string.IsNullOrWhiteSpace(batchReference))
            throw new ArgumentException("Batch reference is required.", nameof(batchReference));

        if (!amount.IsPositive)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        if (paymentCount <= 0)
            throw new ArgumentException("Payment count must be greater than zero.", nameof(paymentCount));

        MerchantId = merchantId;
        BatchReference = batchReference;
        Amount = amount.Amount;
        Currency = amount.Currency;
        PaymentCount = paymentCount;
    }

    public void MarkCompleted(Guid ledgerTransactionId)
    {
        Status = SettlementStatus.Completed;
        LedgerTransactionId = ledgerTransactionId;
    }
}
