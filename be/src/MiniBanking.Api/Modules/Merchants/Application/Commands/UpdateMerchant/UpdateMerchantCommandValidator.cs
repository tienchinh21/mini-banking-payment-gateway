using FluentValidation;

namespace MiniBanking.Modules.Merchants.Application.Commands.UpdateMerchant;

public class UpdateMerchantCommandValidator : AbstractValidator<UpdateMerchantCommand>
{
    public UpdateMerchantCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("ID đối tác không được để trống.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên đối tác không được để trống.")
            .MaximumLength(200).WithMessage("Tên đối tác không được vượt quá 200 ký tự.");

        When(x => !string.IsNullOrWhiteSpace(x.WebhookUrl), () =>
        {
            RuleFor(x => x.WebhookUrl)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                             (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .WithMessage("WebhookUrl phải là đường dẫn URL hợp lệ (http hoặc https).");
        });
    }
}
