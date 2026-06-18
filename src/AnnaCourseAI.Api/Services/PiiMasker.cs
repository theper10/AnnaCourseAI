using System.Text.RegularExpressions;

namespace AnnaCourseAI.Api.Services;

public sealed partial class PiiMasker
{
    public MaskResult Mask(string input, params string?[] names)
    {
        var warnings = new List<string>();
        var masked = input;

        if (EmailRegex().IsMatch(masked))
        {
            masked = EmailRegex().Replace(masked, "[E-POST]");
            warnings.Add("E-postadress maskerad fore AI-anrop.");
        }

        if (PersonNumberRegex().IsMatch(masked))
        {
            masked = PersonNumberRegex().Replace(masked, "[PERSONNUMMER]");
            warnings.Add("Mojligt personnummer maskerat fore AI-anrop.");
        }

        if (PhoneRegex().IsMatch(masked))
        {
            masked = PhoneRegex().Replace(masked, "[TELEFON]");
            warnings.Add("Mojligt telefonnummer maskerat fore AI-anrop.");
        }

        foreach (var name in names.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            var escaped = Regex.Escape(name!.Trim());
            masked = Regex.Replace(masked, escaped, "[NAMN]", RegexOptions.IgnoreCase);
        }

        return new MaskResult(masked, warnings);
    }

    [GeneratedRegex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:19|20)?\d{6}[-+]?\d{4}\b", RegexOptions.Compiled)]
    private static partial Regex PersonNumberRegex();

    [GeneratedRegex(@"\b(?:\+46|0)\s?\d(?:[\s-]?\d){7,10}\b", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();
}

public sealed record MaskResult(string Text, IReadOnlyList<string> Warnings);
