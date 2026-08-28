using MiniBanking.Modules.Payments.Domain;

namespace MiniBanking.SharedKernel.Contracts;

public interface IIdempotencyService
{
    /// <summary>
    /// Checks if a request with this idempotency key was already completed.
    /// Throws InvalidOperationException if the key is reused with a conflicting body hash.
    /// </summary>
    Task<(bool IsCompleted, TResponse? CachedResponse, IdempotencyRecord Record)> CheckOrInitializeAsync<TResponse>(
        string merchantId,
        string idempotencyKey,
        string requestMethod,
        string requestPath,
        string bodyHash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the idempotency record completed with a serialized JSON response.
    /// </summary>
    void Complete<TResponse>(IdempotencyRecord record, TResponse response);

    /// <summary>
    /// Persists the early failure idempotency record before transaction open.
    /// </summary>
    Task SaveEarlyFailureAsync<TResponse>(
        string merchantId,
        string idempotencyKey,
        string requestMethod,
        string requestPath,
        string bodyHash,
        TResponse failureResponse,
        CancellationToken cancellationToken = default);
}
