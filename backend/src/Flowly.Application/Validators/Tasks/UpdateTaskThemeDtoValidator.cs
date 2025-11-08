using Flowly.Application.DTOs.Tasks;
using FluentValidation;

namespace Flowly.Application.Validators.Tasks;

public class UpdateTaskThemeDtoValidator : AbstractValidator<UpdateTaskThemeDto>
{
    public UpdateTaskThemeDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Theme title is required")
            .MaximumLength(100).WithMessage("Theme title must not exceed 100 characters")
            .When(x => x.Title != null);

        RuleFor(x => x.Color)
            .MaximumLength(7).WithMessage("Color must not exceed 7 characters (hex format)")
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Color must be in hex format (e.g., #FF5733)")
            .When(x => !string.IsNullOrWhiteSpace(x.Color));

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be greater than or equal to 0")
            .When(x => x.Order.HasValue);
    }
}
