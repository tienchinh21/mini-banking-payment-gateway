using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Merchants.Domain;

namespace MiniBanking.Modules.Merchants.Application.Commands.UpdateMerchant;

public sealed class UpdateMerchantHandler : IRequestHandler<UpdateMerchantCommand, UpdateMerchantResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public UpdateMerchantHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UpdateMerchantResponse> Handle(UpdateMerchantCommand command, CancellationToken cancellationToken)
    {
        Merchant? merchant;
        if (Guid.TryParse(command.Id, out var g))
            merchant = await _dbContext.Merchants.FirstOrDefaultAsync(m => m.Id == g, cancellationToken);
        else
            merchant = await _dbContext.Merchants.FirstOrDefaultAsync(m => m.MerchantId == command.Id, cancellationToken);

        if (merchant is null)
            throw new KeyNotFoundException("Không tìm thấy đối tác.");

        var webhook = string.IsNullOrWhiteSpace(command.WebhookUrl) ? null : command.WebhookUrl.Trim();
        merchant.UpdateDetails(command.Name.Trim(), webhook, command.IsActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateMerchantResponse(
            merchant.Id,
            merchant.MerchantId,
            merchant.MerchantId,
            merchant.Name,
            merchant.ApiKey,
            merchant.WebhookUrl,
            merchant.IsActive,
            merchant.IsActive ? "ACTIVE" : "SUSPENDED",
            merchant.CreatedAt,
            merchant.UpdatedAt
        );
    }
}
