using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel.Contracts;
using System.Text.Json;

namespace MiniBanking.Modules.Payments.Application.Services;

public class IdempotencyService : IIdempotencyService
{
    private readonly MiniBankingDbContext _dbContext;

    public IdempotencyService(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(bool IsCompleted, TResponse? CachedResponse, IdempotencyRecord Record)> CheckOrInitializeAsync<TResponse>(
        string merchantId,
        string idempotencyKey,
        string requestMethod,
        string requestPath,
        string bodyHash,
        CancellationToken cancellationToken = default)
    {
        var existingRecord = await _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(
                r => r.MerchantId == merchantId && r.Key == idempotencyKey,
                cancellationToken);

        if (existingRecord is not null)
        {
            if (existingRecord.RequestBodyHash != bodyHash)
            {
                throw new InvalidOperationException("Idempotency key was used with a different request body.");
            }

            if (existingRecord.Status == "Completed" && !string.IsNullOrEmpty(existingRecord.ResponsePayload))
            {
                var cached = JsonSerializer.Deserialize<TResponse>(existingRecord.ResponsePayload);
                return (true, cached, existingRecord);
            }

            return (false, default, existingRecord);
        }

        var newRecord = new IdempotencyRecord(
            merchantId,
            idempotencyKey,
            requestMethod,
            requestPath,
            bodyHash);

        _dbContext.IdempotencyRecords.Add(newRecord);
        return (false, default, newRecord);
    }

    public void Complete<TResponse>(IdempotencyRecord record, TResponse response)
    {
        record.Complete(JsonSerializer.Serialize(response));
    }

    public async Task SaveEarlyFailureAsync<TResponse>(
        string merchantId,
        string idempotencyKey,
        string requestMethod,
        string requestPath,
        string bodyHash,
        TResponse failureResponse,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(
                r => r.MerchantId == merchantId && r.Key == idempotencyKey,
                cancellationToken);

        var record = existing ?? new IdempotencyRecord(
            merchantId,
            idempotencyKey,
            requestMethod,
            requestPath,
            bodyHash);

        if (existing is null)
            _dbContext.IdempotencyRecords.Add(record);

        record.Complete(JsonSerializer.Serialize(failureResponse));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
