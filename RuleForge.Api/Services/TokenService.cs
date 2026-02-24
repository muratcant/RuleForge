using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RuleForge.Api.Models;
using RuleForge.Api.Options;

namespace RuleForge.Api.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _jwt;

    public TokenService(JwtSettings jwt)
    {
        _jwt = jwt;
    }

    public TokenResponse CreateToken(string role = "Admin")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTimeOffset.UtcNow.AddHours(24);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "dev-user"),
            new(ClaimTypes.Name, "Dev User"),
            new(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            _jwt.Issuer,
            _jwt.Audience,
            claims,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenResponse(jwt, expires);
    }
}
