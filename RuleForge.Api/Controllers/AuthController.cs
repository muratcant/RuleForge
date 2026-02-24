using Microsoft.AspNetCore.Mvc;
using RuleForge.Api.Models;
using RuleForge.Api.Services;

namespace RuleForge.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ITokenService tokenService, IWebHostEnvironment env) : ControllerBase
{
    /// <summary>Development only: returns a JWT Bearer token (default role: Admin).</summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult CreateToken([FromBody] TokenRequest? request = null)
    {
        if (!env.IsDevelopment())
            return NotFound();

        var response = tokenService.CreateToken(request?.Role ?? "Admin");
        return Ok(response);
    }
}
