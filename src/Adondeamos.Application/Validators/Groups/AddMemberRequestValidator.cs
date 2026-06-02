using Adondeamos.Application.DTOs.Groups;
using FluentValidation;

namespace Adondeamos.Application.Validators.Groups;

public sealed class AddMemberRequestValidator : AbstractValidator<AddMemberRequest>
{
    public AddMemberRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Indica el correo o el id del usuario a agregar.");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El correo no tiene un formato válido.");
        });
    }
}
