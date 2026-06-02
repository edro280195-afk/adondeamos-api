using Adondeamos.Application.DTOs.Lists;
using FluentValidation;

namespace Adondeamos.Application.Validators.Lists;

public sealed class AddListItemRequestValidator : AbstractValidator<AddListItemRequest>
{
    public AddListItemRequestValidator()
    {
        RuleFor(x => x.SaveId)
            .NotEmpty().WithMessage("El save_id es obligatorio.");
    }
}
