using AnnaCourseAI.Api.Data;
using AnnaCourseAI.Api.Models;
using AnnaCourseAI.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICourseRepository, InMemoryCourseRepository>();
builder.Services.AddSingleton<RagSearchService>();
builder.Services.AddSingleton<PromptSafetyService>();
builder.Services.AddSingleton<PiiMasker>();
builder.Services.AddSingleton<DemoAiDraftService>();
builder.Services.AddHttpClient<OpenAiCompatibleDraftService>();
builder.Services.AddScoped<IAiDraftService>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var provider = configuration["AI_PROVIDER"] ?? configuration["AiProvider"];
    var apiKey = configuration["OPENAI_API_KEY"] ?? configuration["OpenAI:ApiKey"];

    if (!string.IsNullOrWhiteSpace(apiKey) &&
        string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
    {
        return serviceProvider.GetRequiredService<OpenAiCompatibleDraftService>();
    }

    return serviceProvider.GetRequiredService<DemoAiDraftService>();
});
builder.Services.AddScoped<AssistWorkflow>();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ai", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30;
        limiterOptions.QueueLimit = 0;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.AutoReplenishment = true;
    });
});

var app = builder.Build();

app.UseRateLimiter();

var configuredApiKey = app.Configuration["ANNA_API_KEY"];
app.Use(async (context, next) =>
{
    if (string.IsNullOrWhiteSpace(configuredApiKey) ||
        !context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
        apiKey != configuredApiKey)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Missing or invalid X-Api-Key header."
        });
        return;
    }

    await next();
});

app.MapGet("/", () => Results.Ok(new
{
    service = "AnnaCourseAI",
    purpose = "AI-stod for kursmaterial, feedback och studentfragor.",
    mode = app.Configuration["AI_PROVIDER"] ?? "Demo",
    endpoints = new[]
    {
        "GET /api/courses",
        "GET /api/courses/{courseId}/materials",
        "POST /api/courses/{courseId}/materials",
        "POST /api/assist/question",
        "POST /api/assist/exercise",
        "POST /api/assist/feedback",
        "POST /api/automation/student-question"
    }
}));

var api = app.MapGroup("/api");

api.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    utc = DateTimeOffset.UtcNow
}));

api.MapGet("/courses", (ICourseRepository repository) =>
{
    return Results.Ok(repository.GetCourses());
});

api.MapGet("/courses/{courseId}/materials", (string courseId, ICourseRepository repository) =>
{
    if (repository.GetCourse(courseId) is null)
    {
        return Results.NotFound(new { error = $"Course '{courseId}' was not found." });
    }

    return Results.Ok(repository.GetMaterials(courseId));
});

api.MapPost("/courses/{courseId}/materials", (
    string courseId,
    AddMaterialRequest request,
    ICourseRepository repository) =>
{
    if (repository.GetCourse(courseId) is null)
    {
        return Results.NotFound(new { error = $"Course '{courseId}' was not found." });
    }

    if (string.IsNullOrWhiteSpace(request.Title) ||
        string.IsNullOrWhiteSpace(request.Content))
    {
        return Results.BadRequest(new { error = "Title and content are required." });
    }

    var material = repository.AddMaterial(courseId, request);
    return Results.Created($"/api/courses/{courseId}/materials/{material.Id}", material);
});

var assist = api.MapGroup("/assist")
    .RequireRateLimiting("ai");

assist.MapPost("/question", async (
    QuestionAssistRequest request,
    AssistWorkflow workflow,
    CancellationToken cancellationToken) =>
{
    var response = await workflow.AnswerQuestionAsync(request, cancellationToken);
    return Results.Ok(response);
});

assist.MapPost("/exercise", async (
    ExerciseAssistRequest request,
    AssistWorkflow workflow,
    CancellationToken cancellationToken) =>
{
    var response = await workflow.CreateExerciseAsync(request, cancellationToken);
    return Results.Ok(response);
});

assist.MapPost("/feedback", async (
    FeedbackAssistRequest request,
    AssistWorkflow workflow,
    CancellationToken cancellationToken) =>
{
    var response = await workflow.CreateFeedbackAsync(request, cancellationToken);
    return Results.Ok(response);
});

api.MapPost("/automation/student-question", async (
    StudentQuestionAutomationRequest request,
    AssistWorkflow workflow,
    CancellationToken cancellationToken) =>
{
    var response = await workflow.HandleStudentQuestionAutomationAsync(request, cancellationToken);
    return Results.Ok(response);
}).RequireRateLimiting("ai");

app.Run();

public partial class Program;
