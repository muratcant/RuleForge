namespace RuleForge.Application.Evaluate.Dto;

public sealed class EvaluationResult
{
    public IReadOnlyList<MatchedRuleResult> MatchedRules { get; set; } = [];
}
