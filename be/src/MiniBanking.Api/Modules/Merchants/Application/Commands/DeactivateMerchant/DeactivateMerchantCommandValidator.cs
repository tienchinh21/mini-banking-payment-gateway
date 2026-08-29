using FluentValidation;

namespace MiniBanking.Modules.Merchants.Application.Commands.DeactivateMerchant;

public class DeactivateMerchantCommandValidator : AbstractValidator<DeactivateMerchantCommand>
{
    public DeactivateMerchantCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID đối tác không được để trống.");
    }
}
