using System.Security.Cryptography;
using System.Text;

namespace Locatarius.Infrastructure.Auth;

public static class SessionTokenGenerator
{
    public static string GenerateToken()
    {
        var bytes = new byte[32]; // 256-bit CSPRNG token
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string HashToken(string rawToken)
    {
        var bytes = Encoding.ASCII.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}