using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Merchants.Domain;

namespace MiniBanking.Modules.Merchants.Application.Commands.CreateMerchant;

public sealed class CreateMerchantHandler : IRequestHandler<CreateMerchantCommand, CreateMerchantResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public CreateMerchantHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CreateMerchantResponse> Handle(CreateMerchantCommand command, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Merchants
            .AsNoTracking()
            .AnyAsync(m => m.MerchantId == command.MerchantId, cancellationToken);

        if (exists)
            throw new InvalidOperationException("MerchantId đã tồn tại trong hệ thống.");

        var apiKey = $"mb_live_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}";
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

        var merchant = new Merchant(
            command.MerchantId.Trim(),
            command.Name.Trim(),
            apiKey,
            secret,
            string.IsNullOrWhiteSpace(command.WebhookUrl) ? null : command.WebhookUrl.Trim());

        _dbContext.Merchants.Add(merchant);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateMerchantResponse(
            merchant.Id,
            merchant.MerchantId,
            merchant.MerchantId,
            merchant.Name,
            merchant.ApiKey,
            merchant.Secret,
            merchant.WebhookUrl,
            merchant.IsActive,
            merchant.IsActive ? "ACTIVE" : "SUSPENDED",
            merchant.CreatedAt
        );
    }
}
