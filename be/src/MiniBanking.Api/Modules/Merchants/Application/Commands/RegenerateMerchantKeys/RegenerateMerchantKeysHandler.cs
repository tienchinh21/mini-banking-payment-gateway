using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Merchants.Domain;

namespace MiniBanking.Modules.Merchants.Application.Commands.RegenerateMerchantKeys;

public sealed class RegenerateMerchantKeysHandler : IRequestHandler<RegenerateMerchantKeysCommand, RegenerateMerchantKeysResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public RegenerateMerchantKeysHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RegenerateMerchantKeysResponse> Handle(RegenerateMerchantKeysCommand command, CancellationToken cancellationToken)
    {
        Merchant? merchant;
        if (Guid.TryParse(command.Id, out var g))
            merchant = await _dbContext.Merchants.FirstOrDefaultAsync(m => m.Id == g, cancellationToken);
        else
            merchant = await _dbContext.Merchants.FirstOrDefaultAsync(m => m.MerchantId == command.Id, cancellationToken);

        if (merchant is null)
            throw new KeyNotFoundException("Không tìm thấy đối tác.");

        var newApiKey = $"mb_live_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}";
        var newSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        merchant.RegenerateCredentials(newApiKey, newSecret);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RegenerateMerchantKeysResponse(
            merchant.Id,
            merchant.MerchantId,
            merchant.MerchantId,
            merchant.ApiKey,
            merchant.Secret,
            merchant.UpdatedAt
        );
    }
}
