using System;
using Xunit;

namespace Locatarius.Infrastructure.Tests
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _passwordHasher;

        public PasswordHasherTests()
        {
            _passwordHasher = new PasswordHasher();
        }

        [Fact]
        public void HashPassword_ShouldThrowExceptionForNullPassword()
        {
            Assert.Throws<ArgumentNullException>(() => _passwordHasher.HashPassword(null));
        }

        [Fact]
        public void HashPassword_ShouldThrowExceptionForEmptyPassword()
        {
            Assert.Throws<ArgumentException>(() => _passwordHasher.HashPassword(string.Empty));
        }

        [Fact]
        public void HashPassword_ShouldGenerateUniqueHashesForSamePasswordWithDifferentSalts()
        {
            string password = "SecurePassword123!";
            string hash1 = _passwordHasher.HashPassword(password);
            string hash2 = _passwordHasher.HashPassword(password);

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void HashPassword_ShouldGenerateConsistentHashFormat()
        {
            string password = "SecurePassword123!";
            string hash = _passwordHasher.HashPassword(password);

            string[] parts = hash.Split('.');
            Assert.Equal(2, parts.Length); // Ensure salt and hash are present
        }

        [Fact]
        public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
        {
            string password = "SecurePassword123!";
            string hash = _passwordHasher.HashPassword(password);

            bool result = _passwordHasher.VerifyPassword(hash, password);
            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_ShouldReturnFalseForIncorrectPassword()
        {
            string password = "SecurePassword123!";
            string hash = _passwordHasher.HashPassword(password);

            bool result = _passwordHasher.VerifyPassword(hash, "WrongPassword!");
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_ShouldReturnFalseForNullPassword()
        {
            string hash = _passwordHasher.HashPassword("SecurePassword123!");
            bool result = _passwordHasher.VerifyPassword(hash, null);

            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_ShouldReturnFalseForEmptyPassword()
        {
            string hash = _passwordHasher.HashPassword("SecurePassword123!");
            bool result = _passwordHasher.VerifyPassword(hash, string.Empty);

            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_ShouldReturnFalseForNullHash()
        {
            bool result = _passwordHasher.VerifyPassword(null, "SecurePassword123!");

            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_ShouldReturnFalseForEmptyHash()
        {
            bool result = _passwordHasher.VerifyPassword(string.Empty, "SecurePassword123!");

            Assert.False(result);
        }

        [Fact]
        public void NeedsRehash_ShouldReturnTrueForInvalidHashFormat()
        {
            string invalidHash = "InvalidHashFormat";
            bool result = _passwordHasher.NeedsRehash(invalidHash);

            Assert.True(result);
        }

        [Fact]
        public void NeedsRehash_ShouldReturnTrueForOutdatedSaltOrHashSize()
        {
            string outdatedHash = $"{Convert.ToBase64String(new byte[8])}.{Convert.ToBase64String(new byte[16])}";
            bool result = _passwordHasher.NeedsRehash(outdatedHash);

            Assert.True(result);
        }

        [Fact]
        public void NeedsRehash_ShouldReturnFalseForValidHash()
        {
            string password = "SecurePassword123!";
            string hash = _passwordHasher.HashPassword(password);

            bool result = _passwordHasher.NeedsRehash(hash);
            Assert.False(result);
        }

        [Fact]
        public void HashPassword_ShouldHandleSpecialCharacters()
        {
            string password = "P@$$w0rd!#";
            string hash = _passwordHasher.HashPassword(password);

            bool result = _passwordHasher.VerifyPassword(hash, password);
            Assert.True(result);
        }

        [Fact]
        public void HashPassword_ShouldHandleLongPasswords()
        {
            string password = new string('a', 1000); // 1000-character password
            string hash = _passwordHasher.HashPassword(password);

            bool result = _passwordHasher.VerifyPassword(hash, password);
            Assert.True(result);
        }

        [Fact]
        public void HashPassword_ShouldHandleShortPasswords()
        {
            string password = "a";
            string hash = _passwordHasher.HashPassword(password);

            bool result = _passwordHasher.VerifyPassword(hash, password);
            Assert.True(result);
        }

        [Fact]
        public void HashPassword_ShouldGenerateDifferentHashesForDifferentPasswords()
        {
            string password1 = "Password1!";
            string password2 = "Password2!";

            string hash1 = _passwordHasher.HashPassword(password1);
            string hash2 = _passwordHasher.HashPassword(password2);

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void VerifyPassword_ShouldBeCaseSensitive()
        {
            string password = "SecurePassword123!";
            string hash = _passwordHasher.HashPassword(password);

            bool result = _passwordHasher.VerifyPassword(hash, "securepassword123!");
            Assert.False(result);
        }

        [Fact]
        public void NeedsRehash_ShouldReturnTrueForCorruptedHash()
        {
            string password = "SecurePassword123!";
            string hash = _passwordHasher.HashPassword(password);

            // Corrupt the hash
            string corruptedHash = hash.Substring(0, hash.Length - 1) + "A";

            bool result = _passwordHasher.NeedsRehash(corruptedHash);
            Assert.True(result);
        }
    }
}
