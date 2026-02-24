using RuleForge.Domain.Rules;

namespace RuleForge.Application.Rules.Dto;

public sealed class RuleDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    public bool IsActive { get; set; }

    public int Priority { get; set; }

    public ConditionDto? Conditions { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public static RuleDto FromEntity(Rule rule, ConditionDto? conditions)
    {
        return new RuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            IsActive = rule.IsActive,
            Priority = rule.Priority,
            Conditions = conditions,
            CreatedAtUtc = rule.CreatedAtUtc,
            UpdatedAtUtc = rule.UpdatedAtUtc
        };
    }
}

