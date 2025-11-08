using Flowly.Application.DTOs.Tasks;
using FluentValidation;

namespace Flowly.Application.Validators.Tasks;

public class CreateRecurrenceDtoValidator : AbstractValidator<CreateRecurrenceDto>
{
    public CreateRecurrenceDtoValidator()
    {
        RuleFor(x => x.Rule)
            .NotEmpty().WithMessage("Recurrence rule is required")
            .MaximumLength(500).WithMessage("Recurrence rule must not exceed 500 characters");
    }
}
