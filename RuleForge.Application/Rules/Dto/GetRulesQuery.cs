namespace RuleForge.Application.Rules.Dto;

public sealed class GetRulesQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? SortBy { get; set; }

    public string? SortDir { get; set; }

    public bool? IsActive { get; set; }

    public int? MinPriority { get; set; }

    public int? MaxPriority { get; set; }

    public string? Search { get; set; }
}

