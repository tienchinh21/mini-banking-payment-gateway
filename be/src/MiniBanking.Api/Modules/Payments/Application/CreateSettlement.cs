using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;
using MiniBanking.SharedKernel.Behaviors;
using MiniBanking.SharedKernel.Contracts;
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

public sealed class CreateSettlementCommand : IRequest<CreateSettlementResponse>, ITransactionalRequest
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
    private readonly ILedgerPostingService _ledgerPostingService;

    public CreateSettlementCommandHandler(
        MiniBankingDbContext dbContext,
        ILedgerPostingService ledgerPostingService)
    {
        _dbContext = dbContext;
        _ledgerPostingService = ledgerPostingService;
    }

    public async Task<CreateSettlementResponse> Handle(CreateSettlementCommand command, CancellationToken cancellationToken)
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

        // Post Settlement via Ledger Domain Service
        var ledgerTx = await _ledgerPostingService.PostSettlementAsync(
            command.Request.MerchantId, amount, new Money(0, "VND"), $"Settlement batch {command.Request.BatchReference}", cancellationToken);

        settlement.MarkCompleted(ledgerTx.Id);

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

        return new CreateSettlementResponse(
            settlement.Id,
            settlement.BatchReference,
            settlement.MerchantId,
            "Completed",
            settlement.Amount,
            settlement.Currency,
            settlement.PaymentCount);
    }
}
