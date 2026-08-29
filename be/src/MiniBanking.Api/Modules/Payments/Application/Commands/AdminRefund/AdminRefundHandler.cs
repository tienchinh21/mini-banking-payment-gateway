using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Payments.Application.CreateRefund;

namespace MiniBanking.Modules.Payments.Application.Commands.AdminRefund;

public sealed class AdminRefundHandler : IRequestHandler<AdminRefundCommand, CreateRefundResponse>
{
    private readonly MiniBankingDbContext _dbContext;
    private readonly IMediator _mediator;

    public AdminRefundHandler(MiniBankingDbContext dbContext, IMediator mediator)
    {
        _dbContext = dbContext;
        _mediator = mediator;
    }

    public async Task<CreateRefundResponse> Handle(AdminRefundCommand command, CancellationToken cancellationToken)
    {
        var payment = await _dbContext.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == command.PaymentId, cancellationToken);

        if (payment is null)
        {
            throw new KeyNotFoundException("Không tìm thấy giao dịch.");
        }

        var refundAmount = command.Amount ?? payment.Amount;
        var refundReq = new CreateRefundRequest(
            $"ADM-REF-{Guid.NewGuid():N}"[..20],
            payment.Id,
            refundAmount,
            payment.Currency,
            command.Reason ?? "Admin manual refund");

        var refundCommand = new CreateRefundCommand(
            payment.MerchantId,
            $"adm-idemp-{Guid.NewGuid():N}",
            "POST",
            $"/api/v1/admin/payments/{payment.Id}/refund",
            JsonSerializer.Serialize(refundReq),
            refundReq);

        return await _mediator.Send(refundCommand, cancellationToken);
    }
}
