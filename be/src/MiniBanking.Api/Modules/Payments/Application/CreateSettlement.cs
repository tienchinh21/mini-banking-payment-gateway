using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;
using System.Text.Json;

namespace MiniBanking.Modules.Payments.Application;

public sealed record CreateSettlementRequest(
    string MerchantId,
    string BatchReference);

public sealed record CreateSettlementResponse(
    Guid SettlementId,
    string BatchReference,
    string MerchantId,
    string Status,
    long Amount,
    string Currency,
    int PaymentCount);

public sealed class CreateSettlementCommand : IRequest<CreateSettlementResponse>
{
    public CreateSettlementRequest Request { get; }

    public CreateSettlementCommand(CreateSettlementRequest request)
    {
        Request = request;
    }
}

public sealed class CreateSettlementCommandHandler : IRequestHandler<CreateSettlementCommand, CreateSettlementResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public CreateSettlementCommandHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateSettlementResponse> Handle(CreateSettlementCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var merchant = await _dbContext.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MerchantId == command.Request.MerchantId, cancellationToken);

            if (merchant is null)
                throw new InvalidOperationException("Merchant not found.");

            var existing = await _dbContext.Settlements
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.MerchantId == command.Request.MerchantId &&
                         s.BatchReference == command.Request.BatchReference,
                    cancellationToken);

            if (existing is not null)
                throw new InvalidOperationException("Settlement batch already processed.");

            var paymentsTotal = await _dbContext.Payments
                .AsNoTracking()
                .Where(p => p.MerchantId == command.Request.MerchantId && p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => p.Amount, cancellationToken);

            var refundsTotal = await _dbContext.Refunds
                .AsNoTracking()
                .Where(r => r.MerchantId == command.Request.MerchantId && r.Status == RefundStatus.Succeeded)
                .SumAsync(r => r.Amount, cancellationToken);

            var paymentCount = await _dbContext.Payments
                .AsNoTracking()
                .CountAsync(
                    p => p.MerchantId == command.Request.MerchantId && p.Status == PaymentStatus.Succeeded,
                    cancellationToken);

            var netAmount = paymentsTotal - refundsTotal;

            if (netAmount <= 0)
                throw new InvalidOperationException("Net settlement amount must be positive.");

            var amount = new Money(netAmount, "VND");
            var settlement = new Settlement(
                command.Request.MerchantId,
                command.Request.BatchReference,
                amount,
                paymentCount);

            _dbContext.Settlements.Add(settlement);

            var ledgerTransaction = new LedgerTransaction(
                $"SETTLEMENT-{settlement.Id}",
                LedgerTransactionType.Settlement,
                $"Settlement batch {command.Request.BatchReference}");

            ledgerTransaction.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", amount, isDebit: true);
            ledgerTransaction.AddEntry(SystemAccountIds.MerchantSettlement, "MerchantSettlement", amount, isDebit: false);
            ledgerTransaction.ValidateInvariant();

            _dbContext.LedgerTransactions.Add(ledgerTransaction);

            settlement.MarkCompleted(ledgerTransaction.Id);

            var outboxMessage = new OutboxMessage(
                "SettlementCompleted",
                JsonSerializer.Serialize(new
                {
                    SettlementId = settlement.Id,
                    MerchantId = settlement.MerchantId,
                    BatchReference = settlement.BatchReference,
                    Amount = settlement.Amount,
                    Currency = settlement.Currency,
                    PaymentCount = settlement.PaymentCount,
                    Timestamp = DateTime.UtcNow
                }));

            _dbContext.OutboxMessages.Add(outboxMessage);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new CreateSettlementResponse(
                settlement.Id,
                settlement.BatchReference,
                settlement.MerchantId,
                "Completed",
                settlement.Amount,
                settlement.Currency,
                settlement.PaymentCount);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
