namespace TsLaser.Crm.Api.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string? Password { get; init; }

    public string? PasswordHash { get; init; }
}
