using FluentValidation;

namespace MiniBanking.Modules.Payments.Application.CreateSettlement;

public class CreateSettlementCommandValidator : AbstractValidator<CreateSettlementCommand>
{
    public CreateSettlementCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request payload không được null.");

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.MerchantId)
                .NotEmpty().WithMessage("MerchantId không được để trống.");

            RuleFor(x => x.Request.BatchReference)
                .NotEmpty().WithMessage("BatchReference không được để trống.");
        });
    }
}
