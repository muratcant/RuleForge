namespace RuleForge.Api.Models;

public sealed record TokenResponse(string Token, DateTimeOffset ExpiresAt);
