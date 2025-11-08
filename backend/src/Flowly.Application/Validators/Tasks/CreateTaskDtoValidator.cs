using Flowly.Application.DTOs.Tasks;
using Flowly.Domain.Enums;
using FluentValidation;

namespace Flowly.Application.Validators.Tasks;

public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
{
    public CreateTaskDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value")
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.Color)
            .MaximumLength(7).WithMessage("Color must not exceed 7 characters (hex format)")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be in hex format (e.g., #FF5733)")
            .When(x => !string.IsNullOrWhiteSpace(x.Color));

        // Allow past due dates, so users can create overdue tasks

        RuleFor(x => x.TagIds)
            .Must(tags => tags == null || tags.Count <= 20)
            .WithMessage("Cannot assign more than 20 tags to a task");
    }
}
