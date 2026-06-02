using Adondeamos.Application.DTOs.Auth;
using FluentValidation;

namespace Adondeamos.Application.Validators.Auth;

public sealed class UpdateMeRequestValidator : AbstractValidator<UpdateMeRequest>
{
    public UpdateMeRequestValidator()
    {
        // El nombre es opcional, pero si viene no puede quedar vacío.
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre no puede quedar vacío.")
                .MaximumLength(120).WithMessage("El nombre no puede exceder 120 caracteres.");
        });

        // El avatar es opcional; si viene con contenido debe ser una URL http/https válida.
        When(x => !string.IsNullOrEmpty(x.AvatarUrl), () =>
        {
            RuleFor(x => x.AvatarUrl)
                .MaximumLength(2048).WithMessage("La URL del avatar es demasiado larga.")
                .Must(BeAValidHttpUrl).WithMessage("La URL del avatar no es válida.");
        });
    }

    private static bool BeAValidHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
