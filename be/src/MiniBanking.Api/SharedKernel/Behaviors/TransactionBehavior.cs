using MediatR;
using Microsoft.Extensions.Logging;
using MiniBanking.Infrastructure.Persistence;

namespace MiniBanking.SharedKernel.Behaviors;

public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalRequest
{
    private readonly MiniBankingDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        MiniBankingDbContext dbContext,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // If a transaction is already active, don't nest another
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await next();
        }

        _logger.LogInformation("Beginning database transaction for {RequestName}", requestName);
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Committed database transaction for {RequestName}", requestName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for {RequestName}. Rolling back.", requestName);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
