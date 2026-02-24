namespace RuleForge.Application.Rules.Dto;

public sealed class CreateRuleRequest
{
    public string Name { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public int Priority { get; set; }

    public ConditionDto Conditions { get; set; } = default!;
}

