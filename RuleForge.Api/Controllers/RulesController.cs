using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RuleForge.Application.Common;
using RuleForge.Application.Rules;
using RuleForge.Application.Rules.Dto;

namespace RuleForge.Api.Controllers;

[ApiController]
[Route("api/rules")]
[Authorize(Roles = "Admin")]
public sealed class RulesController(IRuleService ruleService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RuleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<RuleDto>>> Get([FromQuery] GetRulesQuery query, CancellationToken cancellationToken)
    {
        var result = await ruleService.GetAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RuleDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var rule = await ruleService.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return NotFound(Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Rule not found",
                detail: $"Rule with id '{id}' was not found."));
        }

        return Ok(rule);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RuleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RuleDto>> Create([FromBody] CreateRuleRequest request, CancellationToken cancellationToken)
    {
        var created = await ruleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RuleDto>> Update(Guid id, [FromBody] UpdateRuleRequest request, CancellationToken cancellationToken)
    {
        var updated = await ruleService.UpdateAsync(id, request, cancellationToken);
        if (updated is null)
        {
            return NotFound(Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Rule not found",
                detail: $"Rule with id '{id}' was not found."));
        }

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await ruleService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound(Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Rule not found",
                detail: $"Rule with id '{id}' was not found."));
        }

        return NoContent();
    }
}

