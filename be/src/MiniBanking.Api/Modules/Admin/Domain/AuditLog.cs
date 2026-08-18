using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Admin.Domain;

public class AuditLog : Entity
{
    public string ActorId { get; private set; } = string.Empty;
    public string ActorEmail { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string Method { get; private set; } = string.Empty;
    public string Path { get; private set; } = string.Empty;
    public string? RequestBody { get; private set; }
    public int ResponseStatusCode { get; private set; }
    public string? IpAddress { get; private set; }
    public string? CorrelationId { get; private set; }

    private AuditLog() { } // EF Core requires parameterless constructor

    public AuditLog(
        string actorId,
        string actorEmail,
        string action,
        string resource,
        string method,
        string path,
        string? requestBody,
        int responseStatusCode,
        string? ipAddress,
        string? correlationId)
    {
        ActorId = actorId ?? string.Empty;
        ActorEmail = actorEmail ?? string.Empty;
        Action = action ?? string.Empty;
        Resource = resource ?? string.Empty;
        Method = method ?? string.Empty;
        Path = path ?? string.Empty;
        RequestBody = requestBody;
        ResponseStatusCode = responseStatusCode;
        IpAddress = ipAddress;
        CorrelationId = correlationId;
    }
}
