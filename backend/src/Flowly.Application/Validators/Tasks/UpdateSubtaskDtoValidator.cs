using Flowly.Application.DTOs.Tasks;
using FluentValidation;

namespace Flowly.Application.Validators.Tasks;

public class UpdateSubtaskDtoValidator : AbstractValidator<UpdateSubtaskDto>
{
    public UpdateSubtaskDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Subtask title is required")
            .MaximumLength(200).WithMessage("Subtask title must not exceed 200 characters")
            .When(x => x.Title != null);
    }
}
