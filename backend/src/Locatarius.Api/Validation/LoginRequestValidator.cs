namespace Locatarius.Api.Validation;

using System.Text.RegularExpressions;

public sealed class ValidationResult
{
    public bool IsValid { get; }
    public string? CanonicalEmail { get; }
    public Dictionary<string, List<string>>? Fields { get; }

    private ValidationResult(bool isValid, string? email, Dictionary<string, List<string>>? fields)
    {
        IsValid = isValid;
        CanonicalEmail = email;
        Fields = fields;
    }

    public static ValidationResult Ok(string email) => new(true, email, null);
    public static ValidationResult Failed(Dictionary<string, List<string>> fields) => new(false, null, fields);
}

public static class LoginRequestValidator
{
    private static readonly Regex EmailRegex = new(
        @"\A[a-z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-z0-9](?:[a-z0-9-]*[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)+\z",
        RegexOptions.Compiled);

    public static ValidationResult Validate(string? emailRaw, string? passwordRaw)
    {
        var fields = new Dictionary<string, List<string>>();

        var email = ValidateEmail(emailRaw, fields);
        ValidatePassword(passwordRaw, fields);

        return fields.Count == 0 ? ValidationResult.Ok(email!) : ValidationResult.Failed(fields);
    }

    private static string? ValidateEmail(string? raw, Dictionary<string, List<string>> fields)
    {
        var lowered = (raw ?? string.Empty).Trim(' ').ToLowerInvariant();

        if (lowered.Length == 0)
        {
            Add(fields, "email", "This field is required.");
            return null;
        }

        if (!IsValidEmail(lowered))
        {
            Add(fields, "email", "Enter a valid email address.");
            return null;
        }

        return lowered;
    }

    private static bool IsValidEmail(string email)
    {
        if (email.Length < 3 || email.Length > 254) return false;

        foreach (var c in email)
        {
            if (c > 127) return false;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return false;

        var localPart = email[..atIndex];
        var domainPart = email[(atIndex + 1)..];

        if (localPart.Length > 64) return false;
        if (localPart.StartsWith('.') || localPart.EndsWith('.')) return false;
        if (localPart.Contains("..")) return false;

        foreach (var label in domainPart.Split('.'))
        {
            if (label.Length == 0 || label.Length > 63) return false;
        }

        return EmailRegex.IsMatch(email);
    }

    private static void ValidatePassword(string? raw, Dictionary<string, List<string>> fields)
    {
        if (string.IsNullOrEmpty(raw))
        {
            Add(fields, "password", "This field is required.");
            return;
        }

        if (ContainsInvalidControlOrSurrogate(raw))
        {
            Add(fields, "password", "Remove control characters.");
            return;
        }

        var scalarCount = raw.EnumerateRunes().Count();

        if (scalarCount is < 1 or > 128)
        {
            Add(fields, "password", "Use between 1 and 128 characters.");
        }
    }

    private static bool ContainsInvalidControlOrSurrogate(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (c is >= '\u0000' and <= '\u001F' or >= '\u007F' and <= '\u009F')
            {
                return true;
            }

            if (char.IsSurrogate(c))
            {
                if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1]))
                {
                    i++;
                    continue;
                }
                return true;
            }
        }
        return false;
    }

    private static void Add(Dictionary<string, List<string>> fields, string field, string message)
    {
        if (!fields.TryGetValue(field, out var list))
        {
            list = new List<string>();
            fields[field] = list;
        }
        if (!list.Contains(message)) list.Add(message);
    }
}