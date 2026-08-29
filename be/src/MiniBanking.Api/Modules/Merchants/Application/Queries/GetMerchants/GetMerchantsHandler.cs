using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;

namespace MiniBanking.Modules.Merchants.Application.Queries.GetMerchants;

public sealed class GetMerchantsHandler : IRequestHandler<GetMerchantsQuery, GetMerchantsResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetMerchantsHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetMerchantsResponse> Handle(GetMerchantsQuery query, CancellationToken cancellationToken)
    {
        var p = Math.Max(1, query.Page ?? 1);
        var ps = Math.Clamp(query.PageSize ?? 20, 1, 100);

        var dbQuery = _dbContext.Merchants.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            dbQuery = dbQuery.Where(m =>
                m.MerchantId.ToLower().Contains(s) ||
                m.Name.ToLower().Contains(s));
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(m => m.IsActive == query.IsActive.Value);
        }

        var total = await dbQuery.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(total / (double)ps);

        var merchants = await dbQuery
            .OrderByDescending(m => m.CreatedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var items = merchants.Select(m => new MerchantListItemDto(
            m.Id,
            m.MerchantId,
            m.MerchantId,
            m.Name,
            m.ApiKey,
            MaskSecret(m.Secret),
            m.WebhookUrl,
            m.IsActive,
            m.IsActive ? "ACTIVE" : "SUSPENDED",
            m.CreatedAt,
            m.UpdatedAt
        )).ToList();

        var meta = new PaginationMeta(
            CurrentPage: p,
            PageSize: ps,
            TotalItems: total,
            TotalPages: totalPages,
            HasNext: p * ps < total,
            HasPrevious: p > 1
        );

        return new GetMerchantsResponse(
            Items: items,
            TotalCount: total,
            Page: p,
            PageSize: ps,
            TotalPages: totalPages,
            Meta: meta
        );
    }

    private static string MaskSecret(string secret)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length < 8) return "••••••••";
        return $"{secret[..4]}••••••••{secret[^4..]}";
    }
}
