namespace MiniBanking.SharedKernel.Behaviors;

/// <summary>
/// Marker interface for MediatR requests that require an atomic database transaction.
/// </summary>
public interface ITransactionalRequest
{
}
