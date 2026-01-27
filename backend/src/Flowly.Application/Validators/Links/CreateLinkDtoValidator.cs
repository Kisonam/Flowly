using Flowly.Application.DTOs.Links;
using Flowly.Domain.Enums;
using FluentValidation;

namespace Flowly.Application.Validators.Links;

public class CreateLinkDtoValidator : AbstractValidator<CreateLinkDto>
{
    public CreateLinkDtoValidator()
    {
        RuleFor(x => x.FromType)
            .IsInEnum().WithMessage("Invalid FromType value");

        RuleFor(x => x.ToType)
            .IsInEnum().WithMessage("Invalid ToType value");

        RuleFor(x => x.FromId)
            .NotEmpty().WithMessage("FromId is required");

        RuleFor(x => x.ToId)
            .NotEmpty().WithMessage("ToId is required");

        RuleFor(x => x)
            .Must(dto => !(dto.FromType == dto.ToType && dto.FromId == dto.ToId))
            .WithMessage("Cannot create a link from an entity to itself");
    }
}
