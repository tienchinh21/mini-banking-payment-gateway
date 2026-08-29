using FluentValidation;

namespace MiniBanking.Modules.Merchants.Application.Commands.CreateMerchant;

public class CreateMerchantCommandValidator : AbstractValidator<CreateMerchantCommand>
{
    public CreateMerchantCommandValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty().WithMessage("MerchantId và Name không được để trống.")
            .MaximumLength(100).WithMessage("MerchantId không được vượt quá 100 ký tự.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("MerchantId và Name không được để trống.")
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
