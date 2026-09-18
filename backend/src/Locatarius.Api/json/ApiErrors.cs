using Microsoft.AspNetCore.Mvc;

namespace Locatarius.Api.Json;

public static class ApiErrors
{
    public static IActionResult ValidationFailed(Dictionary<string, List<string>> fields)
        => Envelope(400, "VALIDATION_FAILED", "Validation failed.", fields);

    public static IActionResult InvalidRequest()
        => Envelope(400, "INVALID_REQUEST", "Invalid request.", new());

    public static IActionResult Unauthorized(string code, string message)
        => Envelope(401, code, message, new());

    public static IActionResult Forbidden(string code, string message)
        => Envelope(403, code, message, new());

    public static IActionResult UnsupportedMediaType()
        => Envelope(415, "UNSUPPORTED_MEDIA_TYPE", "Use application/json.", new());

    public static IActionResult PayloadTooLarge()
        => Envelope(413, "PAYLOAD_TOO_LARGE", "Request body exceeds 16 KiB.", new());

    private static IActionResult Envelope(
        int status, string code, string message, Dictionary<string, List<string>> fields)
    {
        var payload = new { error = new { code, message, fields } };
        return new ObjectResult(payload) { StatusCode = status };
    }
}