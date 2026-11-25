using System.Globalization;
using System.Security.Cryptography;

namespace Locatarius.Infrastructure;

public class PasswordHasher
{
    private const string Algorithm = "pbkdf2-sha512";
    private const int Iterations = 220_000;
    private const int LegacyIterations = 10_000;
    private const int MaxSupportedIterations = 10_000_000;
    private const int SaltSize = 16; // 128-bit salt
    private const int HashSize = 32; // 256-bit hash

    public string HashPassword(string password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty or whitespace.", nameof(password));

        byte[] salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA512,
            HashSize);

        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string? hashedPassword, string? password)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(password))
            return false;

        if (!TryParseHash(hashedPassword, out var algorithm, out var iterations, out var salt, out var hash))
            return false;

        if (salt.Length == 0 || hash.Length != HashSize)
            return false;

        byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            algorithm,
            HashSize);

        return CryptographicOperations.FixedTimeEquals(hash, computedHash);
    }

    public bool NeedsRehash(string? hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            return true;

        if (!TryParseHash(hashedPassword, out var algorithm, out var iterations, out var salt, out var hash))
            return true;

        return algorithm != HashAlgorithmName.SHA512
            || iterations < Iterations
            || salt.Length != SaltSize
            || hash.Length != HashSize;
    }

    private static bool TryParseHash(
        string hashedPassword,
        out HashAlgorithmName algorithm,
        out int iterations,
        out byte[] salt,
        out byte[] hash)
    {
        algorithm = default;
        iterations = 0;
        salt = [];
        hash = [];

        if (hashedPassword.Contains('$'))
        {
            string[] parts = hashedPassword.Split('$');
            if (parts.Length != 4 || !string.Equals(parts[0], Algorithm, StringComparison.Ordinal))
                return false;

            if (parts[1].Length == 0 || (parts[1].Length > 1 && parts[1][0] == '0'))
                return false;

            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)
                || iterations <= 0
                || iterations > MaxSupportedIterations)
                return false;

            algorithm = HashAlgorithmName.SHA512;
            return TryDecodeBase64(parts[2], out salt) && TryDecodeBase64(parts[3], out hash);
        }

        string[] legacyParts = hashedPassword.Split('.');
        if (legacyParts.Length != 2)
            return false;

        algorithm = HashAlgorithmName.SHA256;
        iterations = LegacyIterations;
        return TryDecodeBase64(legacyParts[0], out salt) && TryDecodeBase64(legacyParts[1], out hash);
    }

    private static bool TryDecodeBase64(string encoded, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrEmpty(encoded) || encoded.Length % 4 != 0)
            return false;

        foreach (char c in encoded)
        {
            if (char.IsWhiteSpace(c)) return false;
        }

        byte[] buffer = new byte[(encoded.Length / 4) * 3];
        if (!Convert.TryFromBase64String(encoded, buffer, out int bytesWritten))
            return false;

        bytes = bytesWritten == buffer.Length
            ? buffer
            : buffer.AsSpan(0, bytesWritten).ToArray();
        return true;
    }
}
