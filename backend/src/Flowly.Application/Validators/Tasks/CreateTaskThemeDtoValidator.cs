using Flowly.Application.DTOs.Tasks;
using FluentValidation;

namespace Flowly.Application.Validators.Tasks;

public class CreateTaskThemeDtoValidator : AbstractValidator<CreateTaskThemeDto>
{
    public CreateTaskThemeDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Theme title is required")
            .MaximumLength(100).WithMessage("Theme title must not exceed 100 characters");

        RuleFor(x => x.Color)
            .MaximumLength(7).WithMessage("Color must not exceed 7 characters (hex format)")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be in hex format (e.g., #FF5733)")
            .When(x => !string.IsNullOrWhiteSpace(x.Color));
    }
}
