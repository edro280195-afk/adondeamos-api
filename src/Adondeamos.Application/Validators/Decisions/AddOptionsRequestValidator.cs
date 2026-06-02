using Adondeamos.Application.DTOs.Decisions;
using FluentValidation;

namespace Adondeamos.Application.Validators.Decisions;

public sealed class AddOptionsRequestValidator : AbstractValidator<AddOptionsRequest>
{
    public AddOptionsRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.AutoFillFromSaves || x.PlaceIds is { Count: > 0 })
            .WithMessage("Indica al menos un place_id o activa autoFillFromSaves.");

        RuleForEach(x => x.PlaceIds)
            .NotEmpty().WithMessage("Hay un place_id vacío en la lista.");
    }
}
