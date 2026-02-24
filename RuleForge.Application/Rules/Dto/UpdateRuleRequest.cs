namespace RuleForge.Application.Rules.Dto;

public sealed class UpdateRuleRequest
{
    public string Name { get; set; } = default!;

    public bool IsActive { get; set; }

    public int Priority { get; set; }

    public ConditionDto Conditions { get; set; } = default!;
}

