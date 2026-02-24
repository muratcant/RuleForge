using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RuleForge.Application.Rules.Dto;
using RuleForge.Domain.Rules;
using RuleForge.Infrastructure.Evaluate;
using RuleForge.Infrastructure.Persistence;

namespace RuleForge.Tests.Evaluate;

public sealed class EvaluationServiceTests
{
    private RuleForgeDbContext _dbContext = null!;
    private EvaluationService _sut = null!;

    private void Arrange(Func<RuleForgeDbContext, Task>? seed = null)
    {
        var options = new DbContextOptionsBuilder<RuleForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RuleForgeDbContext(options);
        _sut = new EvaluationService(_dbContext);
        seed?.Invoke(_dbContext).GetAwaiter().GetResult();
    }

    private static Rule CreateRule(string name, ConditionDto condition, bool isActive = true, int priority = 0)
    {
        return new Rule
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            Priority = priority,
            Conditions = JsonSerializer.Serialize(condition),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static JsonDocument Json(string json) => JsonDocument.Parse(json);

    // ── No rules ────────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_NoActiveRules_ReturnsEmptyMatchedRules()
    {
        Arrange();

        var result = await _sut.EvaluateAsync(Json("{}"));

        result.MatchedRules.Should().BeEmpty();
    }

    // ── Inactive rules excluded ──────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_InactiveRule_IsNotEvaluated()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Inactive",
                new ConditionDto { Field = "status", Operator = "Equals", Value = "active" },
                isActive: false));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"status":"active"}"""));

        result.MatchedRules.Should().BeEmpty();
    }

    // ── Equals / NotEquals ───────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_EqualsOperator_MatchingValue_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Status Check",
                new ConditionDto { Field = "status", Operator = "Equals", Value = "active" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"status":"active"}"""));

        result.MatchedRules.Should().HaveCount(1);
        result.MatchedRules[0].RuleName.Should().Be("Status Check");
        result.MatchedRules[0].MatchedConditions.Should().HaveCount(1);
        result.MatchedRules[0].MatchedConditions[0].Field.Should().Be("status");
    }

    [Fact]
    public async Task EvaluateAsync_EqualsOperator_NonMatchingValue_ReturnsNoMatch()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Status Check",
                new ConditionDto { Field = "status", Operator = "Equals", Value = "active" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"status":"inactive"}"""));

        result.MatchedRules.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_NotEqualsOperator_MatchingValue_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Not Blocked",
                new ConditionDto { Field = "status", Operator = "NotEquals", Value = "blocked" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"status":"active"}"""));

        result.MatchedRules.Should().HaveCount(1);
    }

    // ── GreaterThan / LessThan ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_GreaterThanOperator_FieldValueIsHigher_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("High Value",
                new ConditionDto { Field = "amount", Operator = "GreaterThan", Value = "1000" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"amount":1500}"""));

        result.MatchedRules.Should().HaveCount(1);
        result.MatchedRules[0].MatchedConditions[0].Reason.Should().Contain("1500").And.Contain("1000");
    }

    [Fact]
    public async Task EvaluateAsync_GreaterThanOperator_FieldValueIsLower_ReturnsNoMatch()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("High Value",
                new ConditionDto { Field = "amount", Operator = "GreaterThan", Value = "1000" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"amount":500}"""));

        result.MatchedRules.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_LessThanOperator_FieldValueIsLower_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Low Score",
                new ConditionDto { Field = "score", Operator = "LessThan", Value = "50" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"score":30}"""));

        result.MatchedRules.Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateAsync_GreaterThanOrEqualsOperator_EqualValue_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Min Age",
                new ConditionDto { Field = "age", Operator = "GreaterThanOrEquals", Value = "18" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"age":18}"""));

        result.MatchedRules.Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateAsync_LessThanOrEqualsOperator_EqualValue_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Max Age",
                new ConditionDto { Field = "age", Operator = "LessThanOrEquals", Value = "65" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"age":65}"""));

        result.MatchedRules.Should().HaveCount(1);
    }

    // ── String operators ─────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_ContainsOperator_FieldContainsValue_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Domain Check",
                new ConditionDto { Field = "email", Operator = "Contains", Value = "@example.com" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"email":"user@example.com"}"""));

        result.MatchedRules.Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateAsync_StartsWithOperator_FieldStartsWithValue_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Prefix Check",
                new ConditionDto { Field = "code", Operator = "StartsWith", Value = "VIP" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"code":"VIP-001"}"""));

        result.MatchedRules.Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateAsync_EndsWithOperator_FieldEndsWithValue_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Suffix Check",
                new ConditionDto { Field = "filename", Operator = "EndsWith", Value = ".pdf" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"filename":"invoice.pdf"}"""));

        result.MatchedRules.Should().HaveCount(1);
    }

    // ── IsNull / IsNotNull ───────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_IsNullOperator_FieldMissing_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Missing Field",
                new ConditionDto { Field = "discount", Operator = "IsNull" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"amount":100}"""));

        result.MatchedRules.Should().HaveCount(1);
        result.MatchedRules[0].MatchedConditions[0].Reason.Should().Contain("null");
    }

    [Fact]
    public async Task EvaluateAsync_IsNullOperator_FieldPresent_ReturnsNoMatch()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Missing Field",
                new ConditionDto { Field = "discount", Operator = "IsNull" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"discount":"10%"}"""));

        result.MatchedRules.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_IsNotNullOperator_FieldPresent_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Has Discount",
                new ConditionDto { Field = "discount", Operator = "IsNotNull" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"discount":"10%"}"""));

        result.MatchedRules.Should().HaveCount(1);
    }

    // ── In operator ──────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_InOperator_FieldValueInList_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Premium Tier",
                new ConditionDto { Field = "tier", Operator = "In", Value = "gold,platinum,diamond" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"tier":"platinum"}"""));

        result.MatchedRules.Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateAsync_InOperator_FieldValueNotInList_ReturnsNoMatch()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Premium Tier",
                new ConditionDto { Field = "tier", Operator = "In", Value = "gold,platinum,diamond" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"tier":"silver"}"""));

        result.MatchedRules.Should().BeEmpty();
    }

    // ── Unknown operator ─────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_UnknownOperator_ReturnsNoMatch()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Unknown Op",
                new ConditionDto { Field = "status", Operator = "FuzzyMatch", Value = "active" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"status":"active"}"""));

        result.MatchedRules.Should().BeEmpty();
    }

    // ── Dot-notation (nested field) ──────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_DotNotationField_ResolvesNestedProperty()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Age Check",
                new ConditionDto { Field = "user.age", Operator = "GreaterThan", Value = "17" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"user":{"age":25}}"""));

        result.MatchedRules.Should().HaveCount(1);
        result.MatchedRules[0].MatchedConditions[0].Field.Should().Be("user.age");
    }

    [Fact]
    public async Task EvaluateAsync_DotNotationField_MissingNestedProperty_ReturnsNoMatch()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Age Check",
                new ConditionDto { Field = "user.age", Operator = "GreaterThan", Value = "17" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"user":{"name":"Alice"}}"""));

        result.MatchedRules.Should().BeEmpty();
    }

    // ── Logical AND ──────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_AndGroup_AllConditionsMatch_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            var condition = new ConditionDto
            {
                LogicalOperator = "And",
                Children =
                [
                    new ConditionDto { Field = "status", Operator = "Equals", Value = "active" },
                    new ConditionDto { Field = "amount", Operator = "GreaterThan", Value = "100" }
                ]
            };
            await db.Rules.AddAsync(CreateRule("Active High Value", condition));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"status":"active","amount":500}"""));

        result.MatchedRules.Should().HaveCount(1);
        result.MatchedRules[0].MatchedConditions.Should().HaveCount(2);
    }

    [Fact]
    public async Task EvaluateAsync_AndGroup_OneConditionFails_ReturnsNoMatch()
    {
        Arrange(async db =>
        {
            var condition = new ConditionDto
            {
                LogicalOperator = "And",
                Children =
                [
                    new ConditionDto { Field = "status", Operator = "Equals", Value = "active" },
                    new ConditionDto { Field = "amount", Operator = "GreaterThan", Value = "100" }
                ]
            };
            await db.Rules.AddAsync(CreateRule("Active High Value", condition));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"status":"active","amount":50}"""));

        result.MatchedRules.Should().BeEmpty();
    }

    // ── Logical OR ───────────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_OrGroup_OneConditionMatches_ReturnsMatchedRule()
    {
        Arrange(async db =>
        {
            var condition = new ConditionDto
            {
                LogicalOperator = "Or",
                Children =
                [
                    new ConditionDto { Field = "tier", Operator = "Equals", Value = "gold" },
                    new ConditionDto { Field = "amount", Operator = "GreaterThan", Value = "10000" }
                ]
            };
            await db.Rules.AddAsync(CreateRule("VIP", condition));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"tier":"gold","amount":500}"""));

        result.MatchedRules.Should().HaveCount(1);
        result.MatchedRules[0].MatchedConditions.Should().HaveCount(1);
    }

    [Fact]
    public async Task EvaluateAsync_OrGroup_NoConditionsMatch_ReturnsNoMatch()
    {
        Arrange(async db =>
        {
            var condition = new ConditionDto
            {
                LogicalOperator = "Or",
                Children =
                [
                    new ConditionDto { Field = "tier", Operator = "Equals", Value = "gold" },
                    new ConditionDto { Field = "amount", Operator = "GreaterThan", Value = "10000" }
                ]
            };
            await db.Rules.AddAsync(CreateRule("VIP", condition));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"tier":"silver","amount":500}"""));

        result.MatchedRules.Should().BeEmpty();
    }

    // ── Priority ordering ────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_MultipleMatchingRules_ReturnedInDescendingPriorityOrder()
    {
        Arrange(async db =>
        {
            await db.Rules.AddRangeAsync(
                CreateRule("Low Priority", new ConditionDto { Field = "x", Operator = "IsNotNull" }, priority: 10),
                CreateRule("High Priority", new ConditionDto { Field = "x", Operator = "IsNotNull" }, priority: 100),
                CreateRule("Mid Priority", new ConditionDto { Field = "x", Operator = "IsNotNull" }, priority: 50));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"x":"value"}"""));

        result.MatchedRules.Should().HaveCount(3);
        result.MatchedRules[0].Priority.Should().Be(100);
        result.MatchedRules[1].Priority.Should().Be(50);
        result.MatchedRules[2].Priority.Should().Be(10);
    }

    // ── Reason content ───────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_MatchedCondition_ReasonContainsFieldOperatorAndValues()
    {
        Arrange(async db =>
        {
            await db.Rules.AddAsync(CreateRule("Amount Rule",
                new ConditionDto { Field = "amount", Operator = "GreaterThan", Value = "100" }));
            await db.SaveChangesAsync();
        });

        var result = await _sut.EvaluateAsync(Json("""{"amount":200}"""));

        var reason = result.MatchedRules[0].MatchedConditions[0].Reason;
        reason.Should().Contain("amount");
        reason.Should().Contain("GreaterThan");
        reason.Should().Contain("100");
    }
}
