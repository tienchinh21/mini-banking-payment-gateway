using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Payments.Domain;

public enum RefundStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3
}

public class Refund : Entity
{
    public string MerchantId { get; private set; } = string.Empty;
    public string MerchantRefundId { get; private set; } = string.Empty;
    public Guid PaymentId { get; private set; }
    public long Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public string Reason { get; private set; } = string.Empty;
    public RefundStatus Status { get; private set; } = RefundStatus.Pending;
    public string? FailureCode { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid? LedgerTransactionId { get; private set; }

    private Refund() { } // EF Core requires parameterless constructor

    public Refund(
        string merchantId,
        string merchantRefundId,
        Guid paymentId,
        Money amount,
        string reason,
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));

        if (string.IsNullOrWhiteSpace(merchantRefundId))
            throw new ArgumentException("Merchant refund id is required.", nameof(merchantRefundId));

        if (paymentId == Guid.Empty)
            throw new ArgumentException("Payment id is required.", nameof(paymentId));

        if (!amount.IsPositive)
            throw new ArgumentException("Amount must be positive.", nameof(amount));

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        MerchantId = merchantId;
        MerchantRefundId = merchantRefundId;
        PaymentId = paymentId;
        Amount = amount.Amount;
        Currency = amount.Currency;
        Reason = reason ?? string.Empty;
        IdempotencyKey = idempotencyKey;
    }

    public void MarkSucceeded(Guid ledgerTransactionId)
    {
        Status = RefundStatus.Succeeded;
        LedgerTransactionId = ledgerTransactionId;
    }

    public void MarkFailed(string failureCode)
    {
        Status = RefundStatus.Failed;
        FailureCode = failureCode;
    }
}
