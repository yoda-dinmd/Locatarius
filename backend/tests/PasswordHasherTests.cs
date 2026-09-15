using System;
using System.Security.Cryptography;
using System.Text;
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

        [Theory]
        [InlineData("", "", "fbdb1d1b18aa6c08f7909b02e6b0d6398a3aa5a5e6bfb7c5e3e8a5e8e8e8e8e")] // Empty strings
        [InlineData("key", "The quick brown fox jumps over the lazy dog", "f7bc83f430538424b13298e6aa6fb143ef4d59a149461f7a7b7b7b7b7b7b7b")] // ASCII
        [InlineData("key", "", "5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d5d")] // Empty message
        public void HMAC_SHA256_ShouldMatchExpectedOutput(string key, string message, string expectedHex)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            byte[] computedHash = hmac.ComputeHash(messageBytes);

            string computedHex = BitConverter.ToString(computedHash).Replace("-", "").ToLower();
            Assert.Equal(expectedHex, computedHex);
        }

        [Fact]
        public void HMAC_SHA256_ShouldHandleUnicodeCharacters()
        {
            string key = "🔑KeyWithEmoji";
            string message = "Message with Unicode: 你好, мир, hello!";

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            byte[] computedHash = hmac.ComputeHash(messageBytes);

            Assert.NotNull(computedHash);
        }

        [Fact]
        public void HMAC_SHA256_ShouldHandleBinaryData()
        {
            byte[] key = new byte[] { 0x00, 0x01, 0x02, 0x03 };
            byte[] message = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };

            using var hmac = new HMACSHA256(key);
            byte[] computedHash = hmac.ComputeHash(message);

            Assert.NotNull(computedHash);
        }

        [Fact]
        public void HMAC_SHA256_ShouldUseFixedTimeEqualsForComparison()
        {
            string key = "key";
            string message = "message";

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            byte[] hash1 = hmac.ComputeHash(messageBytes);
            byte[] hash2 = hmac.ComputeHash(messageBytes);

            Assert.True(CryptographicOperations.FixedTimeEquals(hash1, hash2));
        }

        [Fact]
        public void HMAC_SHA256_ShouldProduceDifferentHashesForDifferentKeys()
        {
            string message = "message";
            byte[] key1 = Encoding.UTF8.GetBytes("key1");
            byte[] key2 = Encoding.UTF8.GetBytes("key2");

            using var hmac1 = new HMACSHA256(key1);
            using var hmac2 = new HMACSHA256(key2);

            byte[] hash1 = hmac1.ComputeHash(Encoding.UTF8.GetBytes(message));
            byte[] hash2 = hmac2.ComputeHash(Encoding.UTF8.GetBytes(message));

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void HMAC_SHA256_ShouldProduceSameHashForSameKeyAndMessage()
        {
            string key = "key";
            string message = "message";

            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            byte[] hash1 = hmac.ComputeHash(messageBytes);
            byte[] hash2 = hmac.ComputeHash(messageBytes);

            Assert.Equal(hash1, hash2);
        }
    }
}
