using FluentValidation;
using RuleForge.Application.Rules.Dto;

namespace RuleForge.Application.Rules.Validation;

public sealed class GetRulesQueryValidator : AbstractValidator<GetRulesQuery>
{
    private static readonly string[] AllowedSortBy =
    [
        nameof(GetRulesQuery.SortBy), // placeholder, overridden below
    ];

    public GetRulesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.SortBy)
            .Must(BeValidSortBy)
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage("SortBy must be one of: Name, Priority, CreatedAtUtc.");

        RuleFor(x => x.SortDir)
            .Must(d => d is null or "asc" or "desc")
            .WithMessage("SortDir must be 'asc' or 'desc'.");
    }

    private static bool BeValidSortBy(string? sortBy)
    {
        return sortBy is "Name" or "Priority" or "CreatedAtUtc";
    }
}

