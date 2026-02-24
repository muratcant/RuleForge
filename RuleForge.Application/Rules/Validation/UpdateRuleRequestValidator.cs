using FluentValidation;
using RuleForge.Application.Rules.Dto;

namespace RuleForge.Application.Rules.Validation;

public sealed class UpdateRuleRequestValidator : AbstractValidator<UpdateRuleRequest>
{
    public UpdateRuleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(1000);

        RuleFor(x => x.Conditions)
            .NotNull()
            .SetValidator(new ConditionDtoValidator());
    }
}

