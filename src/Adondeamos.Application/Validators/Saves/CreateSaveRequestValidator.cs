using Adondeamos.Application.Common.Validation;
using Adondeamos.Application.DTOs.Saves;
using FluentValidation;

namespace Adondeamos.Application.Validators.Saves;

public sealed class CreateSaveRequestValidator : AbstractValidator<CreateSaveRequest>
{
    public CreateSaveRequestValidator()
    {
        RuleFor(x => x.PlaceId)
            .NotEmpty().WithMessage("El place_id es obligatorio.");

        When(x => !string.IsNullOrWhiteSpace(x.SourceUrl), () =>
        {
            RuleFor(x => x.SourceUrl)
                .MaximumLength(2048).WithMessage("El enlace de origen es demasiado largo.")
                .Must(ValidationHelpers.BeAValidHttpUrl).WithMessage("El enlace de origen no es una URL válida.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.ThumbnailUrl), () =>
        {
            RuleFor(x => x.ThumbnailUrl)
                .MaximumLength(2048).WithMessage("La URL de la miniatura es demasiado larga.")
                .Must(ValidationHelpers.BeAValidHttpUrl).WithMessage("La URL de la miniatura no es válida.");
        });

        When(x => x.Note is not null, () =>
        {
            RuleFor(x => x.Note)
                .MaximumLength(2000).WithMessage("La nota no puede exceder 2000 caracteres.");
        });
    }
}
