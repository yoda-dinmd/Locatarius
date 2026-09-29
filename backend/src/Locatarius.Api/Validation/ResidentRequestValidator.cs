using Locatarius.Infrastructure.Residents;

namespace Locatarius.Api.Validation;

public static class ResidentRequestValidator
{
    public static (CreateResidentCommand? Command, Dictionary<string, List<string>> Fields) ValidateCreate(
        IReadOnlyDictionary<string, string?> values)
    {
        string? Value(string key) => values.GetValueOrDefault(key);
        var fields = new Dictionary<string, List<string>>();
        var firstName = (Value("firstName") ?? "").Trim();
        var lastName = (Value("lastName") ?? "").Trim();
        if (firstName.Length is < 1 or > 100) fields["firstName"] = ["Use between 1 and 100 characters."];
        if (lastName.Length is < 1 or > 100) fields["lastName"] = ["Use between 1 and 100 characters."];
        var emailValidation = LoginRequestValidator.Validate(Value("email"), "validation");
        if (emailValidation.Fields?.TryGetValue("email", out var emailErrors) == true)
            fields["email"] = emailErrors;
        if (!Guid.TryParse(Value("apartmentId"), out var apartmentId) || apartmentId == Guid.Empty)
            fields["apartmentId"] = ["Choose an apartment."];
        var password = Value("temporaryPassword");
        foreach (var field in ChangePasswordRequestValidator.Validate(password, Value("confirmPassword")))
            fields[field.Key == "newPassword" ? "temporaryPassword" : field.Key] = field.Value;
        if (string.IsNullOrWhiteSpace(password)) fields["temporaryPassword"] = ["Enter a temporary password."];
        return fields.Count == 0
            ? (new(firstName, lastName, emailValidation.CanonicalEmail!, apartmentId, password!), fields)
            : (null, fields);
    }
}
