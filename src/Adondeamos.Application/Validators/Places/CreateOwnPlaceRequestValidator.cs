using Adondeamos.Application.DTOs.Places;
using FluentValidation;

namespace Adondeamos.Application.Validators.Places;

public sealed class CreateOwnPlaceRequestValidator : AbstractValidator<CreateOwnPlaceRequest>
{
    public CreateOwnPlaceRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del lugar es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage("La latitud debe estar entre -90 y 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage("La longitud debe estar entre -180 y 180.");

        When(x => !string.IsNullOrWhiteSpace(x.City), () =>
        {
            RuleFor(x => x.City)
                .MaximumLength(120).WithMessage("La ciudad no puede exceder 120 caracteres.");
        });
    }
}
