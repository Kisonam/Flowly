using System;
using Flowly.Application.DTOs.Auth;
using FluentValidation;

namespace Flowly.Application.Validators.Auth;

public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileDtoValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MinimumLength(2).WithMessage("Display name must be at least 2 characters")
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters");
    }
}
