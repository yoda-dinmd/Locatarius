using System;
using System.Globalization;
using System.Security.Cryptography;

namespace Locatarius.Infrastructure
{
    public class PasswordHasher
    {
        public const string CurrentAlgorithm = "pbkdf2-sha512";
        public const int CurrentIterations = 220_000;

        private const string LegacyAlgorithm = "pbkdf2-sha256";
        private const int LegacyIterations = 10_000;
        private const int MaxSupportedIterations = 10_000_000;
        private const int SaltSize = 16; // 128-bit salt
        private const int HashSize = 32; // 256-bit hash

        public string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password cannot be empty or whitespace.", nameof(password));

            // Generate salt
            byte[] salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            // Generate hash
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                CurrentIterations,
                HashAlgorithmName.SHA512,
                HashSize
            );

            // Encode algorithm and work factor alongside salt and derived hash
            return $"{CurrentAlgorithm}${CurrentIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string? hashedPassword, string? password)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword)) return false;
            if (string.IsNullOrWhiteSpace(password)) return false;

            if (!TryParseHash(hashedPassword, out _, out var algorithm, out var iterations, out var salt, out var hash))
                return false;

            if (salt.Length == 0 || hash.Length != HashSize)
                return false;

            byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                algorithm,
                HashSize
            );

            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }

        public bool NeedsRehash(string? hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword)) return true;

            if (!TryParseHash(hashedPassword, out var isLegacy, out var algorithm, out var iterations, out var salt, out var hash))
                return true;

            if (isLegacy) return true;
            if (algorithm != HashAlgorithmName.SHA512) return true;
            if (iterations < CurrentIterations) return true;

            return salt.Length != SaltSize || hash.Length != HashSize;
        }

        private static bool TryParseHash(
            string hashedPassword,
            out bool isLegacy,
            out HashAlgorithmName algorithm,
            out int iterations,
            out byte[] salt,
            out byte[] hash)
        {
            isLegacy = false;
            algorithm = default;
            iterations = 0;
            salt = Array.Empty<byte>();
            hash = Array.Empty<byte>();

            if (hashedPassword.Contains('$'))
            {
                string[] parts = hashedPassword.Split('$');
                if (parts.Length != 4) return false;

                if (string.Equals(parts[0], CurrentAlgorithm, StringComparison.Ordinal))
                {
                    algorithm = HashAlgorithmName.SHA512;
                }
                else if (string.Equals(parts[0], LegacyAlgorithm, StringComparison.Ordinal))
                {
                    algorithm = HashAlgorithmName.SHA256;
                }
                else
                {
                    return false;
                }

                if (parts[1].Length == 0 || (parts[1].Length > 1 && parts[1][0] == '0'))
                    return false;

                if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)
                    || iterations <= 0
                    || iterations > MaxSupportedIterations)
                {
                    return false;
                }

                return TryDecodeBase64(parts[2], out salt) && TryDecodeBase64(parts[3], out hash);
            }

            string[] legacyParts = hashedPassword.Split('.');
            if (legacyParts.Length != 2) return false;

            isLegacy = true;
            algorithm = HashAlgorithmName.SHA256;
            iterations = LegacyIterations;

            return TryDecodeBase64(legacyParts[0], out salt) && TryDecodeBase64(legacyParts[1], out hash);
        }

        private static bool TryDecodeBase64(string encoded, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
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
}
