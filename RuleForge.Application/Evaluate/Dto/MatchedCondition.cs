namespace RuleForge.Application.Evaluate.Dto;

public sealed class MatchedCondition
{
    public string Field { get; set; } = default!;

    public string Operator { get; set; } = default!;

    public string? Value { get; set; }

    public string Reason { get; set; } = default!;
}
