using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Payments.Domain;

public enum OutboxMessageStatus
{
    Pending = 1,
    Published = 2,
    Failed = 3
}

public class OutboxMessage : Entity
{
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string? Headers { get; private set; }
    public OutboxMessageStatus Status { get; private set; } = OutboxMessageStatus.Pending;
    public int RetryCount { get; private set; }
    public string? Error { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    private OutboxMessage() { } // EF Core requires parameterless constructor

    public OutboxMessage(string eventType, string payload, string? headers = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type is required.", nameof(eventType));

        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload is required.", nameof(payload));

        EventType = eventType;
        Payload = payload;
        Headers = headers;
    }

    public void MarkPublished()
    {
        Status = OutboxMessageStatus.Published;
        PublishedAt = DateTime.UtcNow;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        Status = OutboxMessageStatus.Failed;
        RetryCount++;
        Error = error;
    }

    public void ResetToPending()
    {
        Status = OutboxMessageStatus.Pending;
        Error = null;
    }
}
