using System;
using System.Security.Cryptography;
using System.Text;

namespace Locatarius.Infrastructure
{
    public class PasswordHasher
    {
        private const int SaltSize = 16; // 128-bit salt
        private const int HashSize = 32; // 256-bit hash
        private const int Iterations = 10000;

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
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );

            // Combine salt and hash
            return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
        }

        public bool VerifyPassword(string hashedPassword, string password)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword)) return false;
            if (string.IsNullOrWhiteSpace(password)) return false;

            string[] parts = hashedPassword.Split('.');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] hash = Convert.FromBase64String(parts[1]);

            byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );

            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }

        public bool NeedsRehash(string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword)) return true;

            string[] parts = hashedPassword.Split('.');
            if (parts.Length != 2) return true;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] hash = Convert.FromBase64String(parts[1]);

            return salt.Length != SaltSize || hash.Length != HashSize;
        }
    }
}
