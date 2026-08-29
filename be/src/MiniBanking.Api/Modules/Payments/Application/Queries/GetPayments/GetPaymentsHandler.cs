using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Payments.Domain;

namespace MiniBanking.Modules.Payments.Application.Queries.GetPayments;

public sealed class GetPaymentsHandler : IRequestHandler<GetPaymentsQuery, PaymentsListResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetPaymentsHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentsListResponse> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        var p = Math.Max(1, request.Page ?? 1);
        var ps = Math.Clamp(request.PageSize ?? 20, 1, 100);

        var query = _dbContext.Payments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<PaymentStatus>(request.Status, true, out var st))
            query = query.Where(x => x.Status == st);

        if (!string.IsNullOrWhiteSpace(request.MerchantId))
            query = query.Where(x => x.MerchantId == request.MerchantId);

        if (request.FromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= request.FromDate.Value.ToUniversalTime());

        if (request.ToDate.HasValue)
            query = query.Where(x => x.CreatedAt <= request.ToDate.Value.ToUniversalTime());

        var total = await query.CountAsync(cancellationToken);
        var payments = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var walletIds = payments.Select(x => x.WalletAccountId).Distinct().ToList();
        var wallets = await _dbContext.WalletAccounts
            .Include(w => w.Customer)
            .AsNoTracking()
            .Where(w => walletIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, cancellationToken);

        var items = payments.Select(x =>
        {
            wallets.TryGetValue(x.WalletAccountId, out var w);
            return new PaymentSummaryDto(
                x.Id,
                x.MerchantId,
                x.MerchantOrderId,
                x.WalletAccountId,
                w?.AccountNumber,
                w?.Customer.FullName,
                w?.Customer.Email,
                x.Amount,
                x.Currency,
                x.Status.ToString(),
                x.FailureCode,
                x.Description,
                x.IdempotencyKey,
                x.LedgerTransactionId,
                x.CreatedAt,
                x.UpdatedAt);
        }).ToList();

        var totalPages = (int)Math.Ceiling(total / (double)ps);

        return new PaymentsListResponse(items, total, p, ps, totalPages);
    }
}
