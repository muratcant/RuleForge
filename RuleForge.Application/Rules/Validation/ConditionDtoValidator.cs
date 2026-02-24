using FluentValidation;
using RuleForge.Application.Rules.Dto;

namespace RuleForge.Application.Rules.Validation;

public sealed class ConditionDtoValidator : AbstractValidator<ConditionDto>
{
    public ConditionDtoValidator()
    {
        RuleFor(x => x.Field)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Operator)
            .NotEmpty()
            .MaximumLength(50);

        RuleForEach(x => x.Children!)
            .SetValidator(this)
            .When(x => x.Children is not null && x.Children.Count > 0);
    }
}

