using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuleForge.Application.Evaluate;
using RuleForge.Application.Evaluate.Dto;

namespace RuleForge.Api.Controllers;

[ApiController]
[Route("api/evaluate")]
[Authorize(Roles = "User")]
public sealed class EvaluateController : ControllerBase
{
    private readonly IEvaluationService _evaluationService;

    public EvaluateController(IEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(EvaluationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EvaluationResult>> Evaluate([FromBody] JsonDocument input, CancellationToken cancellationToken)
    {
        var result = await _evaluationService.EvaluateAsync(input, cancellationToken);
        return Ok(result);
    }
}
