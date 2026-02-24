using RuleForge.Api.Models;

namespace RuleForge.Api.Services;

public interface ITokenService
{
    TokenResponse CreateToken(string role = "Admin");
}
