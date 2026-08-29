using FluentValidation;

namespace MiniBanking.Modules.Merchants.Application.Commands.RegenerateMerchantKeys;

public class RegenerateMerchantKeysCommandValidator : AbstractValidator<RegenerateMerchantKeysCommand>
{
    public RegenerateMerchantKeysCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID đối tác không được để trống.");
    }
}
