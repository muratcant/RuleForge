namespace RuleForge.Application.Evaluate.Dto;

public sealed class MatchedRuleResult
{
    public Guid RuleId { get; set; }

    public string RuleName { get; set; } = default!;

    public int Priority { get; set; }

    public IReadOnlyList<MatchedCondition> MatchedConditions { get; set; } = [];
}
