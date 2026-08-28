using MediatR;
using MiniBanking.SharedKernel.Behaviors;

namespace MiniBanking.Modules.Payments.Application.CreatePayment;

public sealed class CreatePaymentCommand : IRequest<PaymentResponse>, ITransactionalRequest
{
    public string MerchantId { get; }
    public string IdempotencyKey { get; }
    public string RequestMethod { get; }
    public string RequestPath { get; }
    public string RequestBody { get; }
    public CreatePaymentRequest Request { get; }

    public CreatePaymentCommand(
        string merchantId,
        string idempotencyKey,
        string requestMethod,
        string requestPath,
        string requestBody,
        CreatePaymentRequest request)
    {
        MerchantId = merchantId;
        IdempotencyKey = idempotencyKey;
        RequestMethod = requestMethod;
        RequestPath = requestPath;
        RequestBody = requestBody;
        Request = request;
    }
}
