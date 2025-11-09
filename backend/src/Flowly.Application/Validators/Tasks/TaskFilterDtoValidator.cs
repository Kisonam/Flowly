using Flowly.Application.DTOs.Tasks;
using Flowly.Domain.Enums;
using FluentValidation;

namespace Flowly.Application.Validators.Tasks;

public class TaskFilterDtoValidator : AbstractValidator<TaskFilterDto>
{
    public TaskFilterDtoValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(200).WithMessage("Search query must not exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value")
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100");

        RuleFor(x => x.TagIds)
            .Must(tags => tags == null || tags.Count <= 20)
            .WithMessage("Cannot filter by more than 20 tags");

        RuleFor(x => x.ThemeIds)
            .Must(themes => themes == null || themes.Count <= 20)
            .WithMessage("Cannot filter by more than 20 themes");
    }
}
