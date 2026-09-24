using Locatarius.Api.Validation;

namespace Locatarius.Infrastructure.Tests;

public sealed class ChangePasswordRequestValidatorTests
{
    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void ValidatesPasswordLength(int length, bool valid)
    {
        var password = new string('a', length);
        Assert.Equal(valid, ChangePasswordRequestValidator.Validate(password, password).Count == 0);
    }

    [Theory]
    [InlineData(null, null, "newPassword")]
    [InlineData("", "", "newPassword")]
    [InlineData("PrivatePassword123!", "PrivatePassword123! ", "confirmPassword")]
    [InlineData("PrivatePassword123!", null, "confirmPassword")]
    [InlineData("Private\nPassword123!", "Private\nPassword123!", "newPassword")]
    public void RejectsInvalidValues(string? password, string? confirmation, string field)
        => Assert.Contains(field, ChangePasswordRequestValidator.Validate(password, confirmation).Keys);

    [Fact]
    public void CountsUnicodeScalarsAndRejectsUnpairedSurrogates()
    {
        var password = string.Concat(Enumerable.Repeat("😀", 128));
        Assert.Empty(ChangePasswordRequestValidator.Validate(password, password));
        var invalid = "PrivatePassword123!" + '\uD800';
        Assert.Contains("newPassword", ChangePasswordRequestValidator.Validate(invalid, invalid).Keys);
    }
}
