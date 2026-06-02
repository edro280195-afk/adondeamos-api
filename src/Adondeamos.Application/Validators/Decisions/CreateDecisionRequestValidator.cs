using Adondeamos.Application.DTOs.Decisions;
using FluentValidation;

namespace Adondeamos.Application.Validators.Decisions;

public sealed class CreateDecisionRequestValidator : AbstractValidator<CreateDecisionRequest>
{
    public CreateDecisionRequestValidator()
    {
        When(x => x.Context is not null, () =>
        {
            RuleFor(x => x.Context)
                .MaximumLength(500).WithMessage("El contexto no puede exceder 500 caracteres.");
        });
    }
}
