using System.Text.Json;
using RuleForge.Application.Evaluate.Dto;

namespace RuleForge.Application.Evaluate;

public interface IEvaluationService
{
    Task<EvaluationResult> EvaluateAsync(JsonDocument input, CancellationToken cancellationToken = default);
}
