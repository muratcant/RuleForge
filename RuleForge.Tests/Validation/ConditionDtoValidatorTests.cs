using FluentAssertions;
using RuleForge.Application.Rules.Dto;
using RuleForge.Application.Rules.Validation;

namespace RuleForge.Tests.Validation;

public sealed class ConditionDtoValidatorTests
{
    private readonly ConditionDtoValidator _sut = new();

    [Fact]
    public void Validate_ValidCondition_ReturnsValid()
    {
        var condition = new ConditionDto { Field = "Amount", Operator = "GreaterThan", Value = "100" };

        var result = _sut.Validate(condition);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyField_ReturnsInvalid()
    {
        var condition = new ConditionDto { Field = "", Operator = "Equals", Value = "test" };

        var result = _sut.Validate(condition);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConditionDto.Field));
    }

    [Fact]
    public void Validate_FieldExceeds200Characters_ReturnsInvalid()
    {
        var condition = new ConditionDto { Field = new string('a', 201), Operator = "Equals", Value = "test" };

        var result = _sut.Validate(condition);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConditionDto.Field));
    }

    [Fact]
    public void Validate_EmptyOperator_ReturnsInvalid()
    {
        var condition = new ConditionDto { Field = "Name", Operator = "", Value = "test" };

        var result = _sut.Validate(condition);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ConditionDto.Operator));
    }

    [Fact]
    public void Validate_NestedChildren_ValidatesRecursively()
    {
        var condition = new ConditionDto
        {
            Field = "Parent",
            Operator = "And",
            Children =
            [
                new ConditionDto { Field = "Child", Operator = "Equals", Value = "value" }
            ]
        };

        var result = _sut.Validate(condition);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NestedChildrenWithInvalidChild_ReturnsInvalid()
    {
        var condition = new ConditionDto
        {
            Field = "Parent",
            Operator = "And",
            Children =
            [
                new ConditionDto { Field = "", Operator = "Equals", Value = "value" }
            ]
        };

        var result = _sut.Validate(condition);

        result.IsValid.Should().BeFalse();
    }
}
