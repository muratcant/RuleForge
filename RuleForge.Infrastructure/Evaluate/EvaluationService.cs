using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RuleForge.Application.Evaluate;
using RuleForge.Application.Evaluate.Dto;
using RuleForge.Application.Rules.Dto;
using RuleForge.Domain.Rules;
using RuleForge.Infrastructure.Persistence;

namespace RuleForge.Infrastructure.Evaluate;

public sealed class EvaluationService(RuleForgeDbContext dbContext, IMemoryCache cache) : IEvaluationService
{
    private const string ActiveRulesCacheKey = "ActiveRules";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<EvaluationResult> EvaluateAsync(JsonDocument input, CancellationToken cancellationToken = default)
    {
        var activeRules = await cache.GetOrCreateAsync(ActiveRulesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await dbContext.Rules
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.Priority)
                .ToListAsync(cancellationToken);
        }) ?? new List<Rule>();

        var matchedRules = new List<MatchedRuleResult>();

        foreach (var rule in activeRules)
        {
            if (string.IsNullOrWhiteSpace(rule.Conditions))
                continue;

            ConditionDto? condition;
            try
            {
                condition = JsonSerializer.Deserialize<ConditionDto>(rule.Conditions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (condition is null)
                continue;

            var matchedConditions = new List<MatchedCondition>();
            var isMatch = EvaluateCondition(condition, input.RootElement, matchedConditions);

            if (isMatch)
            {
                matchedRules.Add(new MatchedRuleResult
                {
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    Priority = rule.Priority,
                    MatchedConditions = matchedConditions
                });
            }
        }

        return new EvaluationResult { MatchedRules = matchedRules };
    }

    private static bool EvaluateCondition(ConditionDto condition, JsonElement root, List<MatchedCondition> matched)
    {
        if (condition.Children is { Count: > 0 })
            return EvaluateGroup(condition, root, matched);

        return EvaluateLeaf(condition, root, matched);
    }

    private static bool EvaluateGroup(ConditionDto condition, JsonElement root, List<MatchedCondition> matched)
    {
        var isAnd = !string.Equals(condition.LogicalOperator, "Or", StringComparison.OrdinalIgnoreCase);

        if (isAnd)
        {
            foreach (var child in condition.Children!)
            {
                if (!EvaluateCondition(child, root, matched))
                    return false;
            }
            return true;
        }
        else
        {
            foreach (var child in condition.Children!)
            {
                if (EvaluateCondition(child, root, matched))
                    return true;
            }
            return false;
        }
    }

    private static bool EvaluateLeaf(ConditionDto condition, JsonElement root, List<MatchedCondition> matched)
    {
        var fieldValue = ResolveField(root, condition.Field);

        var result = ApplyOperator(condition.Operator, fieldValue, condition.Value);

        if (result)
        {
            matched.Add(new MatchedCondition
            {
                Field = condition.Field,
                Operator = condition.Operator,
                Value = condition.Value,
                Reason = BuildReason(condition.Field, condition.Operator, condition.Value, fieldValue)
            });
        }

        return result;
    }

    private static string? ResolveField(JsonElement root, string field)
    {
        var parts = field.Split('.');
        var current = root;

        foreach (var part in parts)
        {
            if (current.ValueKind != JsonValueKind.Object)
                return null;

            if (!current.TryGetProperty(part, out var next))
                return null;

            current = next;
        }

        return current.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => current.GetString(),
            _ => current.GetRawText()
        };
    }

    private static bool ApplyOperator(string @operator, string? fieldValue, string? conditionValue)
    {
        return @operator switch
        {
            "Equals" => string.Equals(fieldValue, conditionValue, StringComparison.OrdinalIgnoreCase),
            "NotEquals" => !string.Equals(fieldValue, conditionValue, StringComparison.OrdinalIgnoreCase),
            "Contains" => fieldValue is not null && conditionValue is not null &&
                          fieldValue.Contains(conditionValue, StringComparison.OrdinalIgnoreCase),
            "StartsWith" => fieldValue is not null && conditionValue is not null &&
                            fieldValue.StartsWith(conditionValue, StringComparison.OrdinalIgnoreCase),
            "EndsWith" => fieldValue is not null && conditionValue is not null &&
                          fieldValue.EndsWith(conditionValue, StringComparison.OrdinalIgnoreCase),
            "IsNull" => fieldValue is null,
            "IsNotNull" => fieldValue is not null,
            "GreaterThan" => TryCompareNumeric(fieldValue, conditionValue, out var gt) && gt > 0,
            "GreaterThanOrEquals" => TryCompareNumeric(fieldValue, conditionValue, out var gte) && gte >= 0,
            "LessThan" => TryCompareNumeric(fieldValue, conditionValue, out var lt) && lt < 0,
            "LessThanOrEquals" => TryCompareNumeric(fieldValue, conditionValue, out var lte) && lte <= 0,
            "In" => fieldValue is not null && conditionValue is not null &&
                    conditionValue.Split(',').Any(v => string.Equals(v.Trim(), fieldValue, StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private static bool TryCompareNumeric(string? fieldValue, string? conditionValue, out int comparison)
    {
        comparison = 0;
        if (fieldValue is null || conditionValue is null)
            return false;

        if (decimal.TryParse(fieldValue, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var fv) &&
            decimal.TryParse(conditionValue, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var cv))
        {
            comparison = fv.CompareTo(cv);
            return true;
        }

        return false;
    }

    private static string BuildReason(string field, string @operator, string? conditionValue, string? actualValue)
    {
        return @operator switch
        {
            "IsNull" => $"Field '{field}' is null",
            "IsNotNull" => $"Field '{field}' is not null (value: '{actualValue}')",
            _ => $"Field '{field}' ('{actualValue}') satisfies {@operator} '{conditionValue}'"
        };
    }
}
