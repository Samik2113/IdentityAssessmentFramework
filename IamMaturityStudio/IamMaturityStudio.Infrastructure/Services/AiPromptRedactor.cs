using System.Text.RegularExpressions;

namespace IamMaturityStudio.Infrastructure.Services;

public interface IAiPromptRedactor
{
    string Redact(string value);
}

public sealed class AiPromptRedactor : IAiPromptRedactor
{
    private static readonly IReadOnlyList<(Regex Pattern, string Replacement)> Rules =
    [
        (new Regex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "[REDACTED_EMAIL]"),
        (new Regex("\\bhttps?://[^\\s)\\]>\"']+", RegexOptions.IgnoreCase | RegexOptions.Compiled), "[REDACTED_URL]"),
        (new Regex(@"\b(?:\+?\d{1,3}[\s.-]?)?(?:\(?\d{2,4}\)?[\s.-]?)\d{3,4}[\s.-]?\d{3,4}\b", RegexOptions.Compiled), "[REDACTED_PHONE]"),
        (new Regex(@"\b[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "[REDACTED_GUID]"),
        (new Regex(@"\b(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)\b", RegexOptions.Compiled), "[REDACTED_IP]"),
        (new Regex(@"\b(?:Organization|Org|Company)\s*:\s*[^,;\n\r]+", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Organization:[REDACTED_ORG]")
    ];

    public string Redact(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var redacted = value;
        foreach (var (pattern, replacement) in Rules)
        {
            redacted = pattern.Replace(redacted, replacement);
        }

        return redacted;
    }
}
