using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Payments.Domain;

public class IdempotencyRecord : Entity
{
    public string MerchantId { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string RequestMethod { get; private set; } = string.Empty;
    public string RequestPath { get; private set; } = string.Empty;
    public string RequestBodyHash { get; private set; } = string.Empty;
    public string? ResponsePayload { get; private set; }
    public string Status { get; private set; } = "Processing";

    private IdempotencyRecord() { } // EF Core requires parameterless constructor

    public IdempotencyRecord(
        string merchantId,
        string key,
        string requestMethod,
        string requestPath,
        string requestBodyHash)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Idempotency key is required.", nameof(key));

        if (string.IsNullOrWhiteSpace(requestMethod))
            throw new ArgumentException("Request method is required.", nameof(requestMethod));

        if (string.IsNullOrWhiteSpace(requestPath))
            throw new ArgumentException("Request path is required.", nameof(requestPath));

        MerchantId = merchantId;
        Key = key;
        RequestMethod = requestMethod;
        RequestPath = requestPath;
        RequestBodyHash = requestBodyHash;
    }

    public void Complete(string responsePayload)
    {
        ResponsePayload = responsePayload;
        Status = "Completed";
    }
}
