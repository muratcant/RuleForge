using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RuleForge.Application.Common;
using RuleForge.Application.Rules;
using RuleForge.Application.Rules.Dto;
using RuleForge.Domain.Rules;
using RuleForge.Infrastructure.Persistence;

namespace RuleForge.Infrastructure.Rules;

public sealed class RuleService(RuleForgeDbContext dbContext, IMemoryCache cache) : IRuleService
{
    private const string ActiveRulesCacheKey = "ActiveRules";

    public async Task<PagedResult<RuleDto>> GetAsync(GetRulesQuery query, CancellationToken cancellationToken = default)
    {
        var rules = dbContext.Rules.AsNoTracking();

        if (query.IsActive.HasValue)
        {
            rules = rules.Where(r => r.IsActive == query.IsActive.Value);
        }

        if (query.MinPriority.HasValue)
        {
            rules = rules.Where(r => r.Priority >= query.MinPriority.Value);
        }

        if (query.MaxPriority.HasValue)
        {
            rules = rules.Where(r => r.Priority <= query.MaxPriority.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            rules = rules.Where(r => EF.Functions.ILike(r.Name, $"%{query.Search}%"));
        }

        var sortDir = string.IsNullOrWhiteSpace(query.SortDir) ? "desc" : query.SortDir!.ToLowerInvariant();
        var sortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "CreatedAtUtc" : query.SortBy!;

        rules = (sortBy, sortDir) switch
        {
            ("Name", "asc") => rules.OrderBy(r => r.Name),
            ("Name", "desc") => rules.OrderByDescending(r => r.Name),
            ("Priority", "asc") => rules.OrderBy(r => r.Priority),
            ("Priority", "desc") => rules.OrderByDescending(r => r.Priority),
            ("CreatedAtUtc", "asc") => rules.OrderBy(r => r.CreatedAtUtc),
            _ => rules.OrderByDescending(r => r.CreatedAtUtc)
        };

        var totalCount = await rules.CountAsync(cancellationToken);

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

        var items = await rules
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtoItems = items
            .Select(r => RuleDto.FromEntity(r, DeserializeConditions(r.Conditions)))
            .ToList();

        return new PagedResult<RuleDto>
        {
            Items = dtoItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<RuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.Rules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        return RuleDto.FromEntity(rule, DeserializeConditions(rule.Conditions));
    }

    public async Task<RuleDto> CreateAsync(CreateRuleRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var rule = new Rule
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            IsActive = request.IsActive,
            Priority = request.Priority,
            Conditions = SerializeConditions(request.Conditions),
            CreatedAtUtc = now,
            UpdatedAtUtc = null
        };

        dbContext.Rules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateCache();

        return RuleDto.FromEntity(rule, request.Conditions);
    }

    public async Task<RuleDto?> UpdateAsync(Guid id, UpdateRuleRequest request, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.Rules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        rule.Name = request.Name;
        rule.IsActive = request.IsActive;
        rule.Priority = request.Priority;
        rule.Conditions = SerializeConditions(request.Conditions);
        rule.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateCache();

        return RuleDto.FromEntity(rule, request.Conditions);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await dbContext.Rules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rule is null)
        {
            return false;
        }

        dbContext.Rules.Remove(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        InvalidateCache();

        return true;
    }

    private void InvalidateCache()
    {
        cache.Remove(ActiveRulesCacheKey);
    }

    private static string SerializeConditions(ConditionDto dto)
    {
        return JsonSerializer.Serialize(dto);
    }

    private static ConditionDto? DeserializeConditions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ConditionDto>(json);
    }
}

