using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Merchants.Domain;

namespace MiniBanking.Modules.Merchants.Application.Commands.DeactivateMerchant;

public sealed class DeactivateMerchantHandler : IRequestHandler<DeactivateMerchantCommand, DeactivateMerchantResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public DeactivateMerchantHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DeactivateMerchantResponse> Handle(DeactivateMerchantCommand command, CancellationToken cancellationToken)
    {
        Merchant? merchant;
        if (Guid.TryParse(command.Id, out var g))
            merchant = await _dbContext.Merchants.FirstOrDefaultAsync(m => m.Id == g, cancellationToken);
        else
            merchant = await _dbContext.Merchants.FirstOrDefaultAsync(m => m.MerchantId == command.Id, cancellationToken);

        if (merchant is null)
            throw new KeyNotFoundException("Không tìm thấy đối tác.");

        merchant.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new DeactivateMerchantResponse(
            merchant.Id,
            merchant.MerchantId,
            merchant.IsActive,
            merchant.IsActive ? "ACTIVE" : "SUSPENDED",
            merchant.UpdatedAt
        );
    }
}
