using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Merchants.Domain;
using MiniBanking.Modules.Payments.Domain;

namespace MiniBanking.Modules.Merchants.Application.Queries.GetMerchantById;

public sealed class GetMerchantByIdHandler : IRequestHandler<GetMerchantByIdQuery, MerchantDetailResponse?>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetMerchantByIdHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MerchantDetailResponse?> Handle(GetMerchantByIdQuery query, CancellationToken cancellationToken)
    {
        Merchant? merchant;
        if (Guid.TryParse(query.Id, out var g))
            merchant = await _dbContext.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.Id == g, cancellationToken);
        else
            merchant = await _dbContext.Merchants.AsNoTracking().FirstOrDefaultAsync(m => m.MerchantId == query.Id, cancellationToken);

        if (merchant is null)
            return null;

        var paymentsCount = await _dbContext.Payments
            .AsNoTracking()
            .CountAsync(p => p.MerchantId == merchant.MerchantId, cancellationToken);

        var totalPaid = await _dbContext.Payments
            .AsNoTracking()
            .Where(p => p.MerchantId == merchant.MerchantId && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (long?)p.Amount, cancellationToken) ?? 0L;

        return new MerchantDetailResponse(
            merchant.Id,
            merchant.MerchantId,
            merchant.MerchantId,
            merchant.Name,
            merchant.ApiKey,
            merchant.Secret,
            merchant.WebhookUrl,
            merchant.IsActive,
            merchant.IsActive ? "ACTIVE" : "SUSPENDED",
            merchant.CreatedAt,
            merchant.UpdatedAt,
            paymentsCount,
            totalPaid,
            totalPaid
        );
    }
}
