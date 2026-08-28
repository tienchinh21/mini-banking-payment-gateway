using FluentValidation;

namespace MiniBanking.Modules.Accounts.Application.TopUpWallet;

public class TopUpWalletCommandValidator : AbstractValidator<TopUpWalletCommand>
{
    public TopUpWalletCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request payload không được null.");

        When(x => x.Request is not null, () =>
        {
            RuleFor(x => x.Request.AccountNumber)
                .NotEmpty().WithMessage("Mã tài khoản không được để trống.");

            RuleFor(x => x.Request.Amount)
                .GreaterThan(0).WithMessage("Số tiền nạp phải lớn hơn 0.");
        });
    }
}
