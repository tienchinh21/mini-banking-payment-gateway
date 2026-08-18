using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Payments.Domain;

public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3
}

public class Payment : Entity
{
    public string MerchantId { get; private set; } = string.Empty;
    public string MerchantOrderId { get; private set; } = string.Empty;
    public Guid WalletAccountId { get; private set; }
    public long Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public string Description { get; private set; } = string.Empty;
    public string? CallbackUrl { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? FailureCode { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid? LedgerTransactionId { get; private set; }

    private Payment() { } // EF Core requires parameterless constructor

    public Payment(
        string merchantId,
        string merchantOrderId,
        Guid walletAccountId,
        Money amount,
        string description,
        string? callbackUrl,
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));

        if (string.IsNullOrWhiteSpace(merchantOrderId))
            throw new ArgumentException("Merchant order id is required.", nameof(merchantOrderId));

        if (walletAccountId == Guid.Empty)
            throw new ArgumentException("Wallet account id is required.", nameof(walletAccountId));

        if (!amount.IsPositive)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        MerchantId = merchantId;
        MerchantOrderId = merchantOrderId;
        WalletAccountId = walletAccountId;
        Amount = amount.Amount;
        Currency = amount.Currency;
        Description = description ?? string.Empty;
        CallbackUrl = callbackUrl;
        IdempotencyKey = idempotencyKey;
    }

    public void MarkSucceeded(Guid ledgerTransactionId)
    {
        Status = PaymentStatus.Succeeded;
        LedgerTransactionId = ledgerTransactionId;
    }

    public void MarkFailed(string failureCode, string? failureMessage = null)
    {
        Status = PaymentStatus.Failed;
        FailureCode = failureCode;
        Description = failureMessage ?? Description;
    }
}
