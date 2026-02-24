using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuleForge.Application.Evaluate;
using RuleForge.Application.Evaluate.Dto;

namespace RuleForge.Api.Controllers;

[ApiController]
[Route("api/evaluate")]
[Authorize(Roles = "Admin,User")]
public sealed class EvaluateController(IEvaluationService evaluationService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(EvaluationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EvaluationResult>> Evaluate([FromBody] JsonDocument input, CancellationToken cancellationToken)
    {
        var result = await evaluationService.EvaluateAsync(input, cancellationToken);
        return Ok(result);
    }
}
