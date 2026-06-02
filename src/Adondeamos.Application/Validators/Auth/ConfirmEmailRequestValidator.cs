using Adondeamos.Application.DTOs.Auth;
using FluentValidation;

namespace Adondeamos.Application.Validators.Auth;

public sealed class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("El token de confirmación es obligatorio.");
    }
}
