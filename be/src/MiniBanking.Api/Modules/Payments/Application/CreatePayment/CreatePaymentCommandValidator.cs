using FluentValidation;

namespace MiniBanking.Modules.Payments.Application.CreatePayment;

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty().WithMessage("MerchantId không được để trống.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey không được để trống.");

        RuleFor(x => x.Request).NotNull().WithMessage("Request payload không được null.");

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.MerchantOrderId)
                .NotEmpty().WithMessage("MerchantOrderId không được để trống.");

            RuleFor(x => x.Request.WalletAccountId)
                .NotEmpty().WithMessage("WalletAccountId không được để trống.")
                .Must(id => Guid.TryParse(id, out _)).WithMessage("WalletAccountId phải là định dạng Guid hợp lệ.");

            RuleFor(x => x.Request.Amount)
                .GreaterThan(0).WithMessage("Số tiền thanh toán phải lớn hơn 0.");

            RuleFor(x => x.Request.Currency)
                .NotEmpty().WithMessage("Loại tiền tệ không được để trống.");
        });
    }
}
