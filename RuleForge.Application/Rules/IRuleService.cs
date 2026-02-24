using RuleForge.Application.Common;
using RuleForge.Application.Rules.Dto;

namespace RuleForge.Application.Rules;

public interface IRuleService
{
    Task<PagedResult<RuleDto>> GetAsync(GetRulesQuery query, CancellationToken cancellationToken = default);

    Task<RuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RuleDto> CreateAsync(CreateRuleRequest request, CancellationToken cancellationToken = default);

    Task<RuleDto?> UpdateAsync(Guid id, UpdateRuleRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

