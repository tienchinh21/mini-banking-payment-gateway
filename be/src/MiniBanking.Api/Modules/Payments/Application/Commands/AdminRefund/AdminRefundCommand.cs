using MediatR;
using MiniBanking.Modules.Payments.Application.CreateRefund;

namespace MiniBanking.Modules.Payments.Application.Commands.AdminRefund;

public sealed record AdminRefundRequest(long? Amount, string? Reason);

public sealed record AdminRefundCommand(
    Guid PaymentId,
    long? Amount,
    string? Reason) : IRequest<CreateRefundResponse>;
