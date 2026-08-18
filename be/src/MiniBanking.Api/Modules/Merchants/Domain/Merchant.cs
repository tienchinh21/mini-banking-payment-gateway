using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Merchants.Domain;

public class Merchant : Entity
{
    public string MerchantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ApiKey { get; private set; } = string.Empty;
    public string Secret { get; private set; } = string.Empty;
    public string? WebhookUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Merchant() { } // EF Core requires parameterless constructor

    public Merchant(string merchantId, string name, string apiKey, string secret, string? webhookUrl = null)
    {
        if (string.IsNullOrWhiteSpace(merchantId))
            throw new ArgumentException("Merchant id is required.", nameof(merchantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key is required.", nameof(apiKey));

        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret is required.", nameof(secret));

        MerchantId = merchantId;
        Name = name;
        ApiKey = apiKey;
        Secret = secret;
        WebhookUrl = webhookUrl;
    }

    public void SetWebhookUrl(string? webhookUrl)
    {
        WebhookUrl = webhookUrl;
    }
}
