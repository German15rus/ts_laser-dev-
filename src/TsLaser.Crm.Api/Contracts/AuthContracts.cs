using System.ComponentModel.DataAnnotations;

namespace TsLaser.Crm.Api.Contracts;

public sealed class LoginRequest
{
    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record LoginResponse(bool Success, string Message);

public sealed record AuthCheckResponse(bool Authenticated);
