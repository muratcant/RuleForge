using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RuleForge.Application.Common;
using RuleForge.Application.Rules;
using RuleForge.Application.Rules.Dto;
using RuleForge.Domain.Rules;
using RuleForge.Infrastructure.Persistence;
using RuleForge.Infrastructure.Rules;

namespace RuleForge.Tests.Services;

public sealed class RuleServiceTests
{
    private RuleForgeDbContext _dbContext = null!;
    private RuleService _sut = null!;

    private void Arrange(Func<RuleForgeDbContext, Task>? seed = null)
    {
        var options = new DbContextOptionsBuilder<RuleForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RuleForgeDbContext(options);
        _sut = new RuleService(_dbContext);
        seed?.Invoke(_dbContext).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetAsync_EmptyDatabase_ReturnsEmptyItemsAndZeroTotalCount()
    {
        Arrange();

        var query = new GetRulesQuery { Page = 1, PageSize = 20 };
        var result = await _sut.GetAsync(query);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_WithIsActiveFilter_ReturnsOnlyMatchingRules()
    {
        Arrange(async db =>
        {
            await db.Rules.AddRangeAsync(
                CreateRule("Active1", isActive: true),
                CreateRule("Active2", isActive: true),
                CreateRule("Inactive1", isActive: false));
            await db.SaveChangesAsync();
        });

        var query = new GetRulesQuery { Page = 1, PageSize = 20, IsActive = true };
        var result = await _sut.GetAsync(query);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(r => r.IsActive);
    }

    [Fact]
    public async Task GetAsync_WithMinPriorityFilter_ReturnsOnlyMatchingRules()
    {
        Arrange(async db =>
        {
            await db.Rules.AddRangeAsync(
                CreateRule("Low", priority: 10),
                CreateRule("Mid", priority: 50),
                CreateRule("High", priority: 100));
            await db.SaveChangesAsync();
        });

        var query = new GetRulesQuery { Page = 1, PageSize = 20, MinPriority = 50 };
        var result = await _sut.GetAsync(query);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(r => r.Priority >= 50);
    }

    [Fact]
    public async Task GetAsync_WithMaxPriorityFilter_ReturnsOnlyMatchingRules()
    {
        Arrange(async db =>
        {
            await db.Rules.AddRangeAsync(
                CreateRule("Low", priority: 10),
                CreateRule("Mid", priority: 50),
                CreateRule("High", priority: 100));
            await db.SaveChangesAsync();
        });

        var query = new GetRulesQuery { Page = 1, PageSize = 20, MaxPriority = 50 };
        var result = await _sut.GetAsync(query);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(r => r.Priority <= 50);
    }

    [Fact]
    public async Task GetAsync_WithSortByNameAsc_ReturnsAscendingOrder()
    {
        Arrange(async db =>
        {
            await db.Rules.AddRangeAsync(
                CreateRule("Charlie"),
                CreateRule("Alpha"),
                CreateRule("Bravo"));
            await db.SaveChangesAsync();
        });

        var query = new GetRulesQuery { Page = 1, PageSize = 20, SortBy = "Name", SortDir = "asc" };
        var result = await _sut.GetAsync(query);

        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Alpha");
        result.Items[1].Name.Should().Be("Bravo");
        result.Items[2].Name.Should().Be("Charlie");
    }

    [Fact]
    public async Task GetAsync_WithPaging_ReturnsCorrectPage()
    {
        Arrange(async db =>
        {
            for (var i = 0; i < 25; i++)
            {
                await db.Rules.AddAsync(CreateRule($"Rule{i}"));
            }
            await db.SaveChangesAsync();
        });

        var query = new GetRulesQuery { Page = 2, PageSize = 10 };
        var result = await _sut.GetAsync(query);

        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRule_ReturnsRuleDto()
    {
        var rule = CreateRule("Test Rule");
        Arrange(async db =>
        {
            await db.Rules.AddAsync(rule);
            await db.SaveChangesAsync();
        });

        var result = await _sut.GetByIdAsync(rule.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Rule");
        result.Id.Should().Be(rule.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingRule_ReturnsNull()
    {
        Arrange();

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesRuleWithCreatedAtUtc()
    {
        Arrange();

        var request = new CreateRuleRequest
        {
            Name = "New Rule",
            IsActive = true,
            Priority = 100,
            Conditions = new ConditionDto { Field = "Amount", Operator = "GreaterThan", Value = "50" }
        };

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("New Rule");
        result.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ExistingRule_UpdatesAndSetsUpdatedAtUtc()
    {
        var rule = CreateRule("Original");
        Arrange(async db =>
        {
            await db.Rules.AddAsync(rule);
            await db.SaveChangesAsync();
        });

        var request = new UpdateRuleRequest
        {
            Name = "Updated",
            IsActive = false,
            Priority = 200,
            Conditions = new ConditionDto { Field = "X", Operator = "Equals", Value = "Y" }
        };

        var result = await _sut.UpdateAsync(rule.Id, request);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
        result.IsActive.Should().BeFalse();
        result.Priority.Should().Be(200);
        result.UpdatedAtUtc.Should().NotBeNull();
        result.UpdatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateAsync_NonExistingRule_ReturnsNull()
    {
        Arrange();

        var request = new UpdateRuleRequest
        {
            Name = "Test",
            IsActive = true,
            Priority = 0,
            Conditions = new ConditionDto { Field = "F", Operator = "O", Value = "V" }
        };

        var result = await _sut.UpdateAsync(Guid.NewGuid(), request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingRule_ReturnsTrueAndRemovesRule()
    {
        var rule = CreateRule("ToDelete");
        Arrange(async db =>
        {
            await db.Rules.AddAsync(rule);
            await db.SaveChangesAsync();
        });

        var result = await _sut.DeleteAsync(rule.Id);

        result.Should().BeTrue();
        var existing = await _dbContext.Rules.FindAsync(rule.Id);
        existing.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingRule_ReturnsFalse()
    {
        Arrange();

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    private static Rule CreateRule(string name, bool isActive = true, int priority = 0)
    {
        return new Rule
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            Priority = priority,
            Conditions = JsonSerializer.Serialize(new ConditionDto { Field = "F", Operator = "O", Value = "V" }),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };
    }
}
