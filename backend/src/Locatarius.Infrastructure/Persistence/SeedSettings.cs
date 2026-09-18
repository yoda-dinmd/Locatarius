using System.Text.RegularExpressions;

namespace Locatarius.Infrastructure.Persistence;

internal static class SeedSettings
{
    public const int NameMaxLength = 100;
    public const int CreationPasswordMinLength = 15;
    public const int CreationPasswordMaxLength = 128;

    private static readonly TimeSpan EmailMatchTimeout =
        TimeSpan.FromMilliseconds(250);

    private static readonly Regex EmailPattern = new(
        @"\A[a-z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-z0-9](?:[a-z0-9-]*[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)+\z",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        EmailMatchTimeout);

    public static string RequireNormalizedEmail(
        string settingName,
        string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            throw new InvalidOperationException(
                $"{settingName} is required.");
        }

        // Reject non-ASCII before case folding (e.g. Kelvin sign must not become k).
        if (email.Any(character => character > 0x7F))
        {
            throw new InvalidOperationException($"{settingName} is invalid.");
        }

        var normalized = TrimAsciiSpaces(email).ToLowerInvariant();

        if (normalized.Length == 0)
        {
            throw new InvalidOperationException(
                $"{settingName} is required.");
        }

        if (!IsValidNormalizedEmail(normalized))
        {
            throw new InvalidOperationException(
                $"{settingName} is invalid.");
        }

        return normalized;
    }

    public static string RequireCreationPassword(
        string settingName,
        string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                $"{settingName} is required.");
        }

        if (HasUnpairedSurrogate(password) || ContainsControlCharacter(password))
        {
            throw new InvalidOperationException(
                $"{settingName} must not contain control characters.");
        }

        var length = password.EnumerateRunes().Count();

        if (length < CreationPasswordMinLength
            || length > CreationPasswordMaxLength)
        {
            throw new InvalidOperationException(
                $"{settingName} must be between {CreationPasswordMinLength} and {CreationPasswordMaxLength} characters.");
        }

        return password;
    }

    public static string RequireName(
        string settingName,
        string? name)
    {
        var trimmed = name?.Trim(' ');

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidOperationException(
                $"{settingName} is required.");
        }

        if (HasUnpairedSurrogate(trimmed) || ContainsControlCharacter(trimmed))
        {
            throw new InvalidOperationException(
                $"{settingName} must not contain control characters.");
        }

        if (trimmed.EnumerateRunes().Count() > NameMaxLength)
        {
            throw new InvalidOperationException(
                $"{settingName} must be between 1 and {NameMaxLength} characters.");
        }

        return trimmed;
    }

    private static bool IsValidNormalizedEmail(string email)
    {
        if (email.Length is < 3 or > 254)
        {
            return false;
        }

        foreach (var character in email)
        {
            if (character > 0x7F)
            {
                return false;
            }
        }

        var separator = email.IndexOf('@');
        if (separator <= 0 || separator != email.LastIndexOf('@'))
        {
            return false;
        }

        var localPart = email[..separator];
        var domain = email[(separator + 1)..];

        if (localPart.Length > 64
            || localPart.StartsWith('.')
            || localPart.EndsWith('.')
            || localPart.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var label in domain.Split('.'))
        {
            if (label.Length is 0 or > 63)
            {
                return false;
            }
        }

        try
        {
            return EmailPattern.IsMatch(email);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static string TrimAsciiSpaces(string value)
    {
        var start = 0;
        var end = value.Length - 1;

        while (start <= end && value[start] == ' ')
        {
            start++;
        }

        while (end >= start && value[end] == ' ')
        {
            end--;
        }

        return start > end
            ? string.Empty
            : value[start..(end + 1)];
    }

    private static bool ContainsControlCharacter(string value)
    {
        foreach (var rune in value.EnumerateRunes())
        {
            var scalar = rune.Value;
            if (scalar <= 0x1F || (scalar >= 0x7F && scalar <= 0x9F))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasUnpairedSurrogate(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (!char.IsSurrogate(value[index]))
            {
                continue;
            }

            if (char.IsHighSurrogate(value[index])
                && index + 1 < value.Length
                && char.IsLowSurrogate(value[index + 1]))
            {
                index++;
                continue;
            }

            return true;
        }

        return false;
    }
}
