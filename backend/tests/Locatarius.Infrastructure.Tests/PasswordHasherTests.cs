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

        private static string CreateLegacySha256Hash(string password, int iterations = 10_000, int saltSize = 16, int hashSize = 32)
        {
            byte[] salt = new byte[saltSize];
            RandomNumberGenerator.Fill(salt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, hashSize);
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private static string CreateFormattedHash(
            string algorithmName,
            HashAlgorithmName hashAlgorithm,
            int iterations,
            string password,
            int saltSize = 16,
            int hashSize = 32)
        {
            byte[] salt = new byte[saltSize];
            RandomNumberGenerator.Fill(salt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithm, hashSize);
            return $"{algorithmName}${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        [Fact]
        public void HashPassword_ShouldThrowExceptionForNullPassword()
        {
            Assert.Throws<ArgumentNullException>(() => _passwordHasher.HashPassword(null!));
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
        public void HashPassword_ShouldUseSelfDescribingSha512FormatWith220000Iterations()
        {
            string password = "SecurePassword123!";
            string storedHash = _passwordHasher.HashPassword(password);

            string[] parts = storedHash.Split('$');
            Assert.Equal(4, parts.Length);
            Assert.Equal("pbkdf2-sha512", parts[0]);
            Assert.Equal("220000", parts[1]);

            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] derived = Convert.FromBase64String(parts[3]);
            Assert.Equal(16, salt.Length);
            Assert.Equal(32, derived.Length);

            byte[] expected = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, 220_000, HashAlgorithmName.SHA512, 32);
            Assert.True(CryptographicOperations.FixedTimeEquals(derived, expected));
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
        public void VerifyPassword_ShouldSupportLegacySha256HashAndMarkForRehash()
        {
            string password = "LegacyPassword123!";
            string legacyHash = CreateLegacySha256Hash(password);

            Assert.True(_passwordHasher.VerifyPassword(legacyHash, password));
            Assert.False(_passwordHasher.VerifyPassword(legacyHash, "WrongPassword!"));
            Assert.True(_passwordHasher.NeedsRehash(legacyHash));
        }

        [Fact]
        public void NeedsRehash_ShouldReturnFalseForUpgradedSha512Hash()
        {
            string password = "SecurePassword123!";
            string hash = _passwordHasher.HashPassword(password);

            Assert.False(_passwordHasher.NeedsRehash(hash));
            Assert.True(_passwordHasher.VerifyPassword(hash, password));
        }

        [Fact]
        public void NeedsRehash_ShouldReturnTrueForMismatchedAlgorithm()
        {
            string password = "SecurePassword123!";
            string sha256Hash = CreateFormattedHash("pbkdf2-sha256", HashAlgorithmName.SHA256, 220_000, password);

            Assert.True(_passwordHasher.VerifyPassword(sha256Hash, password));
            Assert.True(_passwordHasher.NeedsRehash(sha256Hash));
        }

        [Fact]
        public void NeedsRehash_ShouldReturnTrueForIterationCountBelow220000()
        {
            string password = "SecurePassword123!";
            string weakHash = CreateFormattedHash("pbkdf2-sha512", HashAlgorithmName.SHA512, 100_000, password);

            Assert.True(_passwordHasher.VerifyPassword(weakHash, password));
            Assert.True(_passwordHasher.NeedsRehash(weakHash));
        }

        [Fact]
        public void NeedsRehash_ShouldReturnFalseForHigherFutureIterationCount()
        {
            string password = "SecurePassword123!";
            string strongerHash = CreateFormattedHash("pbkdf2-sha512", HashAlgorithmName.SHA512, 250_000, password);

            Assert.True(_passwordHasher.VerifyPassword(strongerHash, password));
            Assert.False(_passwordHasher.NeedsRehash(strongerHash));
        }

        [Theory]
        [InlineData("InvalidHashFormat")]
        [InlineData("not-base64!.not-base64!")]
        [InlineData("AAAAAAAAAAAAAAAAAAAAAA==.not!valid!base64")]
        [InlineData("not!valid!base64.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        [InlineData("pbkdf2-sha512$220000$not!valid$also!invalid")]
        [InlineData("pbkdf2-sha512$220000$AAAAAAAAAAAAAAAAAAAAAA==$not!valid")]
        [InlineData("pbkdf2-sha512$notanumber$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        [InlineData("pbkdf2-sha512$-10$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        [InlineData("pbkdf2-sha512$0$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        [InlineData("pbkdf2-sha512$00220000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        [InlineData("pbkdf2-md5$220000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        [InlineData("pbkdf2-sha512$220000$AAAA$BBBB$extra")]
        [InlineData("pbkdf2-sha512$220000$AAAA")]
        [InlineData("pbkdf2-sha512$220000$AAA A$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
        public void MalformedHashes_ShouldReturnFalseAndNeedsRehashTrueWithoutThrowing(string malformedHash)
        {
            Assert.False(_passwordHasher.VerifyPassword(malformedHash, "SecurePassword123!"));
            Assert.True(_passwordHasher.NeedsRehash(malformedHash));
        }

        [Fact]
        public void NeedsRehash_ShouldReturnTrueForOutdatedSaltOrHashSize()
        {
            string outdatedLegacyHash = $"{Convert.ToBase64String(new byte[8])}.{Convert.ToBase64String(new byte[16])}";
            string outdatedCurrentHash = $"pbkdf2-sha512$220000${Convert.ToBase64String(new byte[8])}${Convert.ToBase64String(new byte[16])}";

            Assert.True(_passwordHasher.NeedsRehash(outdatedLegacyHash));
            Assert.True(_passwordHasher.NeedsRehash(outdatedCurrentHash));
            Assert.False(_passwordHasher.VerifyPassword(outdatedCurrentHash, "SecurePassword123!"));
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

            // Corrupt the trailing base64 padding character
            string corruptedHash = hash.Substring(0, hash.Length - 1) + "A";

            bool result = _passwordHasher.NeedsRehash(corruptedHash);
            Assert.True(result);
            Assert.False(_passwordHasher.VerifyPassword(corruptedHash, password));
        }

        [Theory]
        [InlineData("", "", "b613679a0814d9ec772f95d778c35fc5ff1697c493715653c6c712144292c5ad")] // Empty strings
        [InlineData("key", "The quick brown fox jumps over the lazy dog", "f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8")] // ASCII
        [InlineData("key", "", "5d5d139563c95b5967b9bd9a8c9b233a9dedb45072794cd232dc1b74832607d0")] // Empty message
        public void HMAC_SHA256_ShouldMatchExpectedOutput(string key, string message, string expectedHex)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            byte[] computedHash = hmac.ComputeHash(messageBytes);

            string computedHex = Convert.ToHexStringLower(computedHash);
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
