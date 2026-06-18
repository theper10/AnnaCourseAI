using AnnaCourseAI.Api.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AnnaCourseAI.Api.Services;

public sealed class OpenAiCompatibleDraftService : IAiDraftService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly PromptSafetyService _promptSafetyService;

    public OpenAiCompatibleDraftService(
        HttpClient httpClient,
        IConfiguration configuration,
        PromptSafetyService promptSafetyService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _promptSafetyService = promptSafetyService;

        var baseUrl = _configuration["OPENAI_BASE_URL"] ?? "https://api.openai.com/v1";
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }

    public async Task<string> GenerateDraftAsync(AiDraftRequest request, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["OPENAI_API_KEY"] ?? _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OPENAI_API_KEY saknas.");
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var model = _configuration["OPENAI_MODEL"] ?? "gpt-4o-mini";
        var sourceText = string.Join(
            Environment.NewLine,
            request.Sources.Select(source =>
                $"- {source.Title} ({source.MaterialId}): {source.Snippet}"));

        var payload = new
        {
            model,
            temperature = 0.2,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = _promptSafetyService.BuildSystemInstruction()
                },
                new
                {
                    role = "user",
                    content = $"""
                    Uppgiftstyp: {request.TaskType}

                    Kursmaterial:
                    {sourceText}

                    Sakerhetsvarningar:
                    {string.Join(Environment.NewLine, request.SafetyWarnings)}

                    Anvandarunderlag:
                    {request.UserInput}
                    """
                }
            }
        };

        using var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

        return json.RootElement
                   .GetProperty("choices")[0]
                   .GetProperty("message")
                   .GetProperty("content")
                   .GetString()
               ?? "AI-tjansten returnerade inget innehall.";
    }
}
