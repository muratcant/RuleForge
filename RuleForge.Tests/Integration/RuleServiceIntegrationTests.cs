using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RuleForge.Application.Rules.Dto;
using RuleForge.Domain.Rules;
using RuleForge.Infrastructure.Persistence;
using RuleForge.Infrastructure.Rules;

namespace RuleForge.Tests.Integration;

[Collection("Integration")]
public sealed class RuleServiceIntegrationTests : IDisposable
{
    private readonly RuleForgeDbContext _dbContext;
    private readonly RuleService _sut;

    public RuleServiceIntegrationTests(PostgreSqlFixture fixture)
    {
        var options = new DbContextOptionsBuilder<RuleForgeDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        _dbContext = new RuleForgeDbContext(options);
        _sut = new RuleService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CreateGetByIdUpdateDelete_FullCrudFlow_Succeeds()
    {
        var createRequest = new CreateRuleRequest
        {
            Name = "Integration Rule",
            IsActive = true,
            Priority = 100,
            Conditions = new ConditionDto { Field = "Amount", Operator = "GreaterThan", Value = "100" }
        };

        var created = await _sut.CreateAsync(createRequest);
        created.Should().NotBeNull();
        created!.Name.Should().Be("Integration Rule");

        var fetched = await _sut.GetByIdAsync(created.Id);
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Integration Rule");

        var updateRequest = new UpdateRuleRequest
        {
            Name = "Updated Rule",
            IsActive = false,
            Priority = 200,
            Conditions = new ConditionDto { Field = "Status", Operator = "Equals", Value = "Active" }
        };

        var updated = await _sut.UpdateAsync(created.Id, updateRequest);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Rule");
        updated.IsActive.Should().BeFalse();

        var deleted = await _sut.DeleteAsync(created.Id);
        deleted.Should().BeTrue();

        var afterDelete = await _sut.GetByIdAsync(created.Id);
        afterDelete.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithSearch_PerformsCaseInsensitiveSearch()
    {
        var rule1 = CreateRule("Discount Rule");
        var rule2 = CreateRule("Premium discount");
        var rule3 = CreateRule("Other");
        await _dbContext.Rules.AddRangeAsync(rule1, rule2, rule3);
        await _dbContext.SaveChangesAsync();

        var query = new GetRulesQuery { Page = 1, PageSize = 20, Search = "DISCOUNT" };
        var result = await _sut.GetAsync(query);

        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(r => r.Name == "Discount Rule");
        result.Items.Should().Contain(r => r.Name == "Premium discount");
    }

    [Fact]
    public async Task CreateAsync_WithNestedConditionDto_SerializesToJsonbCorrectly()
    {
        var nestedCondition = new ConditionDto
        {
            Field = "Parent",
            Operator = "And",
            LogicalOperator = "AND",
            Children =
            [
                new ConditionDto { Field = "Child", Operator = "Equals", Value = "value" }
            ]
        };

        var request = new CreateRuleRequest
        {
            Name = "Nested Rule",
            IsActive = true,
            Priority = 50,
            Conditions = nestedCondition
        };

        var created = await _sut.CreateAsync(request);
        created.Should().NotBeNull();
        created.Conditions.Should().NotBeNull();
        created.Conditions!.Children.Should().HaveCount(1);
        created.Conditions.Children![0].Field.Should().Be("Child");

        var fromDb = await _dbContext.Rules.FindAsync(created.Id);
        fromDb.Should().NotBeNull();
        var deserialized = JsonSerializer.Deserialize<ConditionDto>(fromDb!.Conditions);
        deserialized.Should().NotBeNull();
        deserialized!.Children.Should().HaveCount(1);
    }

    private static Rule CreateRule(string name)
    {
        return new Rule
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true,
            Priority = 0,
            Conditions = JsonSerializer.Serialize(new ConditionDto { Field = "F", Operator = "O", Value = "V" }),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };
    }
}
