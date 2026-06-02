using Adondeamos.Application.DTOs.Places;
using FluentValidation;

namespace Adondeamos.Application.Validators.Places;

public sealed class ResolvePlaceRequestValidator : AbstractValidator<ResolvePlaceRequest>
{
    public ResolvePlaceRequestValidator()
    {
        RuleFor(x => x.GooglePlaceId)
            .NotEmpty().WithMessage("El google_place_id es obligatorio.")
            .MaximumLength(512).WithMessage("El google_place_id es demasiado largo.");
    }
}
