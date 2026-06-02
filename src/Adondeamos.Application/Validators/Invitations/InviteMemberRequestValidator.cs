using Adondeamos.Application.DTOs.Invitations;
using FluentValidation;

namespace Adondeamos.Application.Validators.Invitations;

public sealed class InviteMemberRequestValidator : AbstractValidator<InviteMemberRequest>
{
    public InviteMemberRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue || !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Indica el correo o el id del usuario a invitar.");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("El correo no tiene un formato válido.");
        });
    }
}
