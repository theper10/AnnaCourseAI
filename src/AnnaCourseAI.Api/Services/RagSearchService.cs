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

        var start = Math.Max(0, firstHit - 80);
        var length = Math.Min(260, normalized.Length - start);
        var snippet = normalized.Substring(start, length).Trim();

        if (start > 0)
        {
            snippet = "..." + snippet;
        }

        if (start + length < normalized.Length)
        {
            snippet += "...";
        }

        return snippet;
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.Compiled)]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
