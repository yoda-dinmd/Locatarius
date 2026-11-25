using System.Text.Json;
using Locatarius.Infrastructure.Residents;

namespace Locatarius.Api.Validation;

public static class ResidentRequestValidator
{
    public static readonly IReadOnlySet<string> CreateFields = new HashSet<string>
    {
        "firstName", "lastName", "email", "apartmentId", "temporaryPassword", "confirmPassword"
    };

    public static readonly IReadOnlySet<string> UpdateFields = new HashSet<string>
    {
        "firstName", "lastName", "email", "apartmentId", "isActive"
    };

    public static (CreateResidentCommand? Command, Dictionary<string, List<string>> Fields) ValidateCreate(
        IReadOnlyDictionary<string, string?> values)
    {
        string? Value(string key) => values.GetValueOrDefault(key);
        var fields = new Dictionary<string, List<string>>();
        var (firstName, lastName, canonicalEmail, apartmentId) = ValidateProfileFields(
            Value("firstName"), Value("lastName"), Value("email"), Value("apartmentId"), fields);
        var password = Value("temporaryPassword");
        foreach (var field in ChangePasswordRequestValidator.Validate(password, Value("confirmPassword")))
            fields[field.Key == "newPassword" ? "temporaryPassword" : field.Key] = field.Value;
        if (string.IsNullOrWhiteSpace(password)) fields["temporaryPassword"] = ["Enter a temporary password."];
        return fields.Count == 0
            ? (new(firstName, lastName, canonicalEmail!, apartmentId, password!), fields)
            : (null, fields);
    }

    public static (UpdateResidentCommand? Command, Dictionary<string, List<string>> Fields, bool IsInvalidRequest) ValidateUpdate(
        IReadOnlyDictionary<string, JsonElement> values)
    {
        foreach (var (key, value) in values)
        {
            if (!UpdateFields.Contains(key))
                return (null, new(), true);
            if (key == "isActive")
            {
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null))
                    return (null, new(), true);
            }
            else if (value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
            {
                return (null, new(), true);
            }
        }

        string? StringValue(string key)
            => values.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : null;

        var fields = new Dictionary<string, List<string>>();
        var (firstName, lastName, canonicalEmail, apartmentId) = ValidateProfileFields(
            StringValue("firstName"), StringValue("lastName"), StringValue("email"), StringValue("apartmentId"), fields);

        bool? isActive = null;
        if (!values.TryGetValue("isActive", out var activeElement) || activeElement.ValueKind == JsonValueKind.Null)
            fields["isActive"] = ["This field is required."];
        else
            isActive = activeElement.GetBoolean();

        return fields.Count == 0
            ? (new UpdateResidentCommand(firstName, lastName, canonicalEmail!, apartmentId, isActive!.Value), fields, false)
            : (null, fields, false);
    }

    private static (string FirstName, string LastName, string? CanonicalEmail, Guid ApartmentId) ValidateProfileFields(
        string? rawFirstName, string? rawLastName, string? rawEmail, string? rawApartmentId,
        Dictionary<string, List<string>> fields)
    {
        var firstName = (rawFirstName ?? "").Trim();
        var lastName = (rawLastName ?? "").Trim();
        if (firstName.Length is < 1 or > 100) fields["firstName"] = ["Use between 1 and 100 characters."];
        if (lastName.Length is < 1 or > 100) fields["lastName"] = ["Use between 1 and 100 characters."];
        var emailValidation = LoginRequestValidator.Validate(rawEmail, "validation");
        if (emailValidation.Fields?.TryGetValue("email", out var emailErrors) == true)
            fields["email"] = emailErrors;
        if (!Guid.TryParse(rawApartmentId, out var apartmentId) || apartmentId == Guid.Empty)
            fields["apartmentId"] = ["Choose an apartment."];
        return (firstName, lastName, emailValidation.CanonicalEmail, apartmentId);
    }
}
