namespace RuleForge.Application.Rules.Dto;

public sealed class ConditionDto
{
    public string Field { get; set; } = default!;

    public string Operator { get; set; } = default!;

    public string? Value { get; set; }

    public string? LogicalOperator { get; set; }

    public IList<ConditionDto>? Children { get; set; }
}

