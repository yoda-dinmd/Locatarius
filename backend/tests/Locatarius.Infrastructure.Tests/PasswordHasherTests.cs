using System.Security.Cryptography;

namespace Locatarius.Infrastructure.Tests;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher = new();

    private static string CreateLegacySha256Hash(string password)
    {
        byte[] salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    private static string CreateSha512Hash(string password, int iterations)
    {
        byte[] salt = new byte[16];
        RandomNumberGenerator.Fill(salt);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, 32);
        return $"pbkdf2-sha512${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    [Fact]
    public void HashPassword_RejectsNullAndBlankPasswords()
    {
        Assert.Throws<ArgumentNullException>(() => _passwordHasher.HashPassword(null!));
        Assert.Throws<ArgumentException>(() => _passwordHasher.HashPassword(string.Empty));
        Assert.Throws<ArgumentException>(() => _passwordHasher.HashPassword("   "));
    }

    [Fact]
    public void HashPassword_UsesUniqueSaltsAndSelfDescribingSha512FormatWith220000Iterations()
    {
        const string password = "SecurePassword123!";
        string hash1 = _passwordHasher.HashPassword(password);
        string hash2 = _passwordHasher.HashPassword(password);

        Assert.NotEqual(hash1, hash2);

        string[] parts = hash1.Split('$');
        Assert.Equal(4, parts.Length);
        Assert.Equal("pbkdf2-sha512", parts[0]);
        Assert.Equal("220000", parts[1]);

        byte[] salt = Convert.FromBase64String(parts[2]);
        byte[] derived = Convert.FromBase64String(parts[3]);
        Assert.Equal(16, salt.Length);
        Assert.Equal(32, derived.Length);

        byte[] expected = Rfc2898DeriveBytes.Pbkdf2(password, salt, 220_000, HashAlgorithmName.SHA512, 32);
        Assert.True(CryptographicOperations.FixedTimeEquals(derived, expected));
        Assert.False(_passwordHasher.NeedsRehash(hash1));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("P@$$w0rd!#")]
    [InlineData("SecurePassword123!")]
    public void HashAndVerify_SucceedsForValidPasswordsAndRejectsMismatches(string password)
    {
        string hash = _passwordHasher.HashPassword(password);

        Assert.True(_passwordHasher.VerifyPassword(hash, password));
        Assert.False(_passwordHasher.VerifyPassword(hash, password.ToUpperInvariant() + "x"));
        Assert.False(_passwordHasher.VerifyPassword(hash, null));
        Assert.False(_passwordHasher.VerifyPassword(hash, string.Empty));
    }

    [Fact]
    public void HashAndVerify_IsCaseSensitiveAndHandlesLongPasswords()
    {
        string longPassword = new('a', 1000);
        string hash = _passwordHasher.HashPassword(longPassword);

        Assert.True(_passwordHasher.VerifyPassword(hash, longPassword));
        Assert.False(_passwordHasher.VerifyPassword(hash, new string('A', 1000)));
    }

    [Fact]
    public void LegacySha256Hash_VerifiesAndMarksForRehash()
    {
        const string password = "LegacyPassword123!";
        string legacyHash = CreateLegacySha256Hash(password);

        Assert.True(_passwordHasher.VerifyPassword(legacyHash, password));
        Assert.False(_passwordHasher.VerifyPassword(legacyHash, "WrongPassword!"));
        Assert.True(_passwordHasher.NeedsRehash(legacyHash));
    }

    [Fact]
    public void NeedsRehash_ReturnsTrueForIterationsBelow220000AndFalseFor220000OrHigher()
    {
        const string password = "SecurePassword123!";
        string weakHash = CreateSha512Hash(password, iterations: 100_000);
        string strongerHash = CreateSha512Hash(password, iterations: 250_000);

        Assert.True(_passwordHasher.VerifyPassword(weakHash, password));
        Assert.True(_passwordHasher.NeedsRehash(weakHash));

        Assert.True(_passwordHasher.VerifyPassword(strongerHash, password));
        Assert.False(_passwordHasher.NeedsRehash(strongerHash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("InvalidHashFormat")]
    [InlineData("not-base64!.not-base64!")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAA==.not!valid!base64")]
    [InlineData("pbkdf2-sha256$220000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("pbkdf2-sha512$220000$not!valid$also!invalid")]
    [InlineData("pbkdf2-sha512$220000$AAAAAAAAAAAAAAAAAAAAAA==$not!valid")]
    [InlineData("pbkdf2-sha512$notanumber$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("pbkdf2-sha512$-10$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("pbkdf2-sha512$0$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("pbkdf2-sha512$00220000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("pbkdf2-sha512$220000$AAAA$BBBB$extra")]
    [InlineData("pbkdf2-sha512$220000$AAAA")]
    [InlineData("pbkdf2-sha512$220000$AAA A$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    public void MalformedOrUnsupportedHashes_ReturnFalseAndNeedsRehashTrueWithoutThrowing(string? storedHash)
    {
        Assert.False(_passwordHasher.VerifyPassword(storedHash, "SecurePassword123!"));
        Assert.True(_passwordHasher.NeedsRehash(storedHash));
    }

    [Fact]
    public void OutdatedSaltOrCorruptedHashLength_TriggersRehashAndFailsVerification()
    {
        const string password = "SecurePassword123!";
        string wrongSizes = $"pbkdf2-sha512$220000${Convert.ToBase64String(new byte[8])}${Convert.ToBase64String(new byte[16])}";
        string validHash = _passwordHasher.HashPassword(password);
        string corruptedHash = validHash[..^1] + "A";

        Assert.True(_passwordHasher.NeedsRehash(wrongSizes));
        Assert.False(_passwordHasher.VerifyPassword(wrongSizes, password));

        Assert.True(_passwordHasher.NeedsRehash(corruptedHash));
        Assert.False(_passwordHasher.VerifyPassword(corruptedHash, password));
    }
}
