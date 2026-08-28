using FluentValidation;

namespace MiniBanking.Modules.Payments.Application.CreateRefund;

public class CreateRefundCommandValidator : AbstractValidator<CreateRefundCommand>
{
    public CreateRefundCommandValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty().WithMessage("MerchantId không được để trống.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey không được để trống.");

        RuleFor(x => x.Request).NotNull().WithMessage("Request payload không được null.");

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.MerchantRefundId)
                .NotEmpty().WithMessage("MerchantRefundId không được để trống.");

            RuleFor(x => x.Request.PaymentId)
                .NotEmpty().WithMessage("PaymentId không được để trống.");

            RuleFor(x => x.Request.Amount)
                .GreaterThan(0).WithMessage("Số tiền hoàn phải lớn hơn 0.");

            RuleFor(x => x.Request.Currency)
                .NotEmpty().WithMessage("Loại tiền tệ không được để trống.");
        });
    }
}
