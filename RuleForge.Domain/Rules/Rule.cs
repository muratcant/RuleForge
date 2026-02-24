using System;

namespace RuleForge.Domain.Rules;

public class Rule
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    public bool IsActive { get; set; }

    public int Priority { get; set; }

    public string Conditions { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
