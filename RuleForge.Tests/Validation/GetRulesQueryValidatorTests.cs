using FluentAssertions;
using RuleForge.Application.Rules.Dto;
using RuleForge.Application.Rules.Validation;

namespace RuleForge.Tests.Validation;

public sealed class GetRulesQueryValidatorTests
{
    private readonly GetRulesQueryValidator _sut = new();

    [Fact]
    public void Validate_ValidQuery_ReturnsValid()
    {
        var query = new GetRulesQuery { Page = 1, PageSize = 20, SortBy = null };

        var result = _sut.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_PageLessThanOne_ReturnsInvalid()
    {
        var query = new GetRulesQuery { Page = 0, PageSize = 20 };

        var result = _sut.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetRulesQuery.Page));
    }

    [Fact]
    public void Validate_PageSizeLessThanOne_ReturnsInvalid()
    {
        var query = new GetRulesQuery { Page = 1, PageSize = 0 };

        var result = _sut.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetRulesQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeGreaterThan100_ReturnsInvalid()
    {
        var query = new GetRulesQuery { Page = 1, PageSize = 101 };

        var result = _sut.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetRulesQuery.PageSize));
    }

    [Fact]
    public void Validate_InvalidSortBy_ReturnsInvalid()
    {
        var query = new GetRulesQuery { Page = 1, PageSize = 20, SortBy = "Invalid" };

        var result = _sut.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetRulesQuery.SortBy));
    }

    [Fact]
    public void Validate_ValidSortByAndSortDir_ReturnsValid()
    {
        var query = new GetRulesQuery { Page = 1, PageSize = 20, SortBy = "Name", SortDir = "asc" };

        var result = _sut.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidSortDir_ReturnsInvalid()
    {
        var query = new GetRulesQuery { Page = 1, PageSize = 20, SortDir = "invalid" };

        var result = _sut.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetRulesQuery.SortDir));
    }
}
