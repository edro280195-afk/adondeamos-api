using Adondeamos.Application.DTOs.Saves;
using FluentValidation;

namespace Adondeamos.Application.Validators.Saves;

public sealed class UpdateSaveRequestValidator : AbstractValidator<UpdateSaveRequest>
{
    public UpdateSaveRequestValidator()
    {
        When(x => x.Note is not null, () =>
        {
            RuleFor(x => x.Note)
                .MaximumLength(2000).WithMessage("La nota no puede exceder 2000 caracteres.");
        });
    }
}
