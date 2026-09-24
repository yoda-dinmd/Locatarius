namespace Locatarius.Api.Validation;

public static class ChangePasswordRequestValidator
{
    public static Dictionary<string, List<string>> Validate(string? newPassword, string? confirmPassword)
    {
        var fields = new Dictionary<string, List<string>>();
        // Reuse login's Unicode/control-character and maximum-length policy.
        var passwordValidation = LoginRequestValidator.Validate("validation@example.test", newPassword);
        if (passwordValidation.Fields?.TryGetValue("password", out var errors) == true)
            fields["newPassword"] = errors;
        else if (newPassword!.EnumerateRunes().Count() < 15)
            fields["newPassword"] = ["Use between 15 and 128 characters."];

        if (string.IsNullOrEmpty(confirmPassword))
            fields["confirmPassword"] = ["This field is required."];
        else if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            fields["confirmPassword"] = ["Passwords must match."];

        return fields;
    }
}
