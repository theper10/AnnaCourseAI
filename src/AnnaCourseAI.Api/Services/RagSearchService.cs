using AnnaCourseAI.Api.Data;
using AnnaCourseAI.Api.Models;
using System.Text.RegularExpressions;

namespace AnnaCourseAI.Api.Services;

public sealed partial class RagSearchService(ICourseRepository repository)
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "att", "det", "och", "som", "for", "med", "till", "ska", "kan", "inte",
        "har", "hur", "vad", "nar", "var", "den", "ett", "en", "pa", "av"
    };

    public IReadOnlyList<SourceReference> Search(string courseId, string query, int take = 3)
    {
        var queryTokens = Tokenize(query)
            .Where(token => !StopWords.Contains(token))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var ranked = repository.GetMaterials(courseId)
            .Select(material =>
            {
                var searchableText = $"{material.Title} {material.Content}";
                var materialTokens = Tokenize(searchableText);
                var score = materialTokens.Count(queryTokens.Contains);

                if (material.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    score += 3;
                }

                return new SourceReference(
                    material.Id,
                    material.Title,
                    CreateSnippet(material.Content, queryTokens),
                    score);
            })
            .OrderByDescending(source => source.Score)
            .ThenBy(source => source.Title)
            .Take(take)
            .ToList();

        return ranked.Count > 0
            ? ranked
            : repository.GetMaterials(courseId)
                .Take(take)
                .Select(material => new SourceReference(
                    material.Id,
                    material.Title,
                    CreateSnippet(material.Content, queryTokens),
                    0))
                .ToList();
    }

    private static IReadOnlyList<string> Tokenize(string value)
    {
        return WordRegex()
            .Matches(value.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(token => token.Length > 2)
            .ToList();
    }

    private static string CreateSnippet(string content, IReadOnlySet<string> queryTokens)
    {
        var normalized = WhitespaceRegex().Replace(content.Trim(), " ");
        var lower = normalized.ToLowerInvariant();
        var firstHit = queryTokens
            .Select(token => lower.IndexOf(token, StringComparison.OrdinalIgnoreCase))
            .Where(index => index >= 0)
            .DefaultIfEmpty(0)
            .Min();

        var start = FindSnippetStart(normalized, Math.Max(0, firstHit - 80));
        var length = Math.Min(260, normalized.Length - start);
        length = FindSnippetLength(normalized, start, length);
        var snippet = normalized.Substring(start, length).Trim();

        return snippet;
    }

    private static int FindSnippetStart(string text, int preferredStart)
    {
        if (preferredStart <= 0)
        {
            return 0;
        }

        var previousSentence = text.LastIndexOfAny(['.', '!', '?'], preferredStart);
        if (previousSentence >= 0 && preferredStart - previousSentence < 120)
        {
            return Math.Min(text.Length, previousSentence + 1);
        }

        var previousSpace = text.IndexOf(' ', preferredStart);
        return previousSpace >= 0
            ? Math.Min(text.Length, previousSpace + 1)
            : preferredStart;
    }

    private static int FindSnippetLength(string text, int start, int maxLength)
    {
        var end = Math.Min(text.Length, start + maxLength);
        if (end >= text.Length)
        {
            return text.Length - start;
        }

        var lastSentence = text.LastIndexOfAny(['.', '!', '?'], end - 1, end - start);
        if (lastSentence > start && end - lastSentence < 100)
        {
            return lastSentence - start + 1;
        }

        var lastSpace = text.LastIndexOf(' ', end - 1, end - start);
        return lastSpace > start
            ? lastSpace - start
            : maxLength;
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.Compiled)]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
