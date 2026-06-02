using Adondeamos.Application.DTOs.Lists;
using FluentValidation;

namespace Adondeamos.Application.Validators.Lists;

public sealed class CreateListRequestValidator : AbstractValidator<CreateListRequest>
{
    public CreateListRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la lista es obligatorio.")
            .MaximumLength(120).WithMessage("El nombre no puede exceder 120 caracteres.");
    }
}
