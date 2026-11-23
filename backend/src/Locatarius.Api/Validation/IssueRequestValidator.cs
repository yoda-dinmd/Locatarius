using System.Text.Json;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Issues;

namespace Locatarius.Api.Validation;

public static class IssueRequestValidator
{
    public const int TitleMinLength = 5;
    public const int TitleMaxLength = 120;
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 2000;

    public static readonly IReadOnlySet<string> CreateFields = new HashSet<string> { "title", "description", "priority" };
    public static readonly IReadOnlySet<string> UpdateFields = new HashSet<string> { "status", "priority" };

    public static (CreateIssueCommand? Command, Dictionary<string, List<string>> Fields) ValidateCreate(
        IReadOnlyDictionary<string, JsonElement> values)
    {
        var fields = new Dictionary<string, List<string>>();

        var title = ReadString(values, "title", fields)?.Trim();
        if (title is not null)
        {
            if (title.Length is < TitleMinLength or > TitleMaxLength)
                fields["title"] = [$"Use between {TitleMinLength} and {TitleMaxLength} characters."];
            else if (title.Any(char.IsControl))
                fields["title"] = ["Remove control characters."];
        }

        var description = ReadString(values, "description", fields)?.Trim();
        if (description is not null)
        {
            if (description.Length is < DescriptionMinLength or > DescriptionMaxLength)
                fields["description"] = [$"Use between {DescriptionMinLength} and {DescriptionMaxLength} characters."];
            // Line breaks and tabs are legitimate in a multi-line description; other control characters are not.
            else if (description.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
                fields["description"] = ["Remove control characters."];
        }

        var priority = ReadEnum<IssuePriority>(values, "priority", fields, "Choose a priority between 1 and 4.");

        return fields.Count == 0
            ? (new CreateIssueCommand(title!, description!, priority ?? IssuePriority.Medium), fields)
            : (null, fields);
    }

    public static (UpdateIssueCommand? Command, Dictionary<string, List<string>> Fields) ValidateUpdate(
        IReadOnlyDictionary<string, JsonElement> values)
    {
        var fields = new Dictionary<string, List<string>>();
        var status = ReadEnum<IssueStatus>(values, "status", fields, "Choose a status between 1 and 3.");
        var priority = ReadEnum<IssuePriority>(values, "priority", fields, "Choose a priority between 1 and 4.");
        if (fields.Count == 0 && status is null && priority is null)
            fields["request"] = ["Provide a status or a priority."];

        return fields.Count == 0 ? (new UpdateIssueCommand(status, priority), fields) : (null, fields);
    }

    private static string? ReadString(
        IReadOnlyDictionary<string, JsonElement> values, string key, Dictionary<string, List<string>> fields)
    {
        if (!values.TryGetValue(key, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            fields[key] = ["This field is required."];
            return null;
        }
        if (value.ValueKind != JsonValueKind.String)
        {
            fields[key] = ["Use text."];
            return null;
        }
        return value.GetString();
    }

    // Optional numeric enum: absent or null means "not provided". Only defined integer values are accepted.
    private static TEnum? ReadEnum<TEnum>(
        IReadOnlyDictionary<string, JsonElement> values, string key,
        Dictionary<string, List<string>> fields, string message) where TEnum : struct, Enum
    {
        if (!values.TryGetValue(key, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            && Enum.IsDefined(typeof(TEnum), number))
            return (TEnum)Enum.ToObject(typeof(TEnum), number);
        fields[key] = [message];
        return null;
    }
}
