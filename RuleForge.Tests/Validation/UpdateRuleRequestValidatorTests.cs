using FluentAssertions;
using RuleForge.Application.Rules.Dto;
using RuleForge.Application.Rules.Validation;

namespace RuleForge.Tests.Validation;

public sealed class UpdateRuleRequestValidatorTests
{
    private readonly UpdateRuleRequestValidator _sut = new();

    private static ConditionDto ValidCondition => new() { Field = "Amount", Operator = "GreaterThan", Value = "100" };

    [Fact]
    public void Validate_ValidRequest_ReturnsValid()
    {
        var request = new UpdateRuleRequest
        {
            Name = "Test Rule",
            IsActive = true,
            Priority = 500,
            Conditions = ValidCondition
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_ReturnsInvalid()
    {
        var request = new UpdateRuleRequest
        {
            Name = "",
            IsActive = true,
            Priority = 0,
            Conditions = ValidCondition
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateRuleRequest.Name));
    }

    [Fact]
    public void Validate_NameExceeds200Characters_ReturnsInvalid()
    {
        var request = new UpdateRuleRequest
        {
            Name = new string('a', 201),
            IsActive = true,
            Priority = 0,
            Conditions = ValidCondition
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateRuleRequest.Name));
    }

    [Fact]
    public void Validate_PriorityLessThanZero_ReturnsInvalid()
    {
        var request = new UpdateRuleRequest
        {
            Name = "Test",
            IsActive = true,
            Priority = -1,
            Conditions = ValidCondition
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateRuleRequest.Priority));
    }

    [Fact]
    public void Validate_PriorityGreaterThan1000_ReturnsInvalid()
    {
        var request = new UpdateRuleRequest
        {
            Name = "Test",
            IsActive = true,
            Priority = 1001,
            Conditions = ValidCondition
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateRuleRequest.Priority));
    }

    [Fact]
    public void Validate_NullConditions_ReturnsInvalid()
    {
        var request = new UpdateRuleRequest
        {
            Name = "Test",
            IsActive = true,
            Priority = 0,
            Conditions = null!
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateRuleRequest.Conditions));
    }

    [Fact]
    public void Validate_InvalidCondition_ReturnsInvalid()
    {
        var request = new UpdateRuleRequest
        {
            Name = "Test",
            IsActive = true,
            Priority = 0,
            Conditions = new ConditionDto { Field = "", Operator = "Equals", Value = "test" }
        };

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
