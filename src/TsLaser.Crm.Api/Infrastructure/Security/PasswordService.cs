using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.Extensions.Options;
using TsLaser.Crm.Api.Options;

namespace TsLaser.Crm.Api.Infrastructure.Security;

public sealed class PasswordService(IOptions<AuthOptions> options) : IPasswordService
{
    private readonly AuthOptions _options = options.Value;

    public bool Verify(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_options.PasswordHash))
        {
            return BCrypt.Net.BCrypt.Verify(password, _options.PasswordHash);
        }

        if (!string.IsNullOrWhiteSpace(_options.Password))
        {
            return SecureEquals(_options.Password, password);
        }

        return false;
    }

    private static bool SecureEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
