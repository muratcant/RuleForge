using FluentAssertions;
using RuleForge.Application.Rules.Dto;
using RuleForge.Domain.Rules;

namespace RuleForge.Tests.Dto;

public sealed class RuleDtoTests
{
    [Fact]
    public void FromEntity_WithRuleAndConditionDto_MapsAllPropertiesCorrectly()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;
        var rule = new Rule
        {
            Id = id,
            Name = "Test Rule",
            IsActive = true,
            Priority = 100,
            Conditions = "{}",
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = updatedAt
        };
        var conditionDto = new ConditionDto { Field = "Amount", Operator = "GreaterThan", Value = "100" };

        var result = RuleDto.FromEntity(rule, conditionDto);

        result.Id.Should().Be(id);
        result.Name.Should().Be("Test Rule");
        result.IsActive.Should().BeTrue();
        result.Priority.Should().Be(100);
        result.Conditions.Should().Be(conditionDto);
        result.CreatedAtUtc.Should().Be(createdAt);
        result.UpdatedAtUtc.Should().Be(updatedAt);
    }

    [Fact]
    public void FromEntity_WithNullConditions_SetsConditionsToNull()
    {
        var rule = new Rule
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            IsActive = false,
            Priority = 0,
            Conditions = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        var result = RuleDto.FromEntity(rule, null);

        result.Conditions.Should().BeNull();
        result.Id.Should().Be(rule.Id);
        result.Name.Should().Be(rule.Name);
    }
}
