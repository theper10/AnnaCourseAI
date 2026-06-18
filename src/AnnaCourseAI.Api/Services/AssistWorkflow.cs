using AnnaCourseAI.Api.Data;
using AnnaCourseAI.Api.Models;

namespace AnnaCourseAI.Api.Services;

public sealed class AssistWorkflow(
    ICourseRepository repository,
    RagSearchService ragSearchService,
    PromptSafetyService promptSafetyService,
    PiiMasker piiMasker,
    IAiDraftService aiDraftService)
{
    public async Task<AssistResponse> AnswerQuestionAsync(
        QuestionAssistRequest request,
        CancellationToken cancellationToken)
    {
        EnsureCourseExists(request.CourseId);
        var masked = piiMasker.Mask(request.Question);
        var warnings = BuildWarnings(request.Question, masked);
        var sources = ragSearchService.Search(request.CourseId, masked.Text);

        var draft = await aiDraftService.GenerateDraftAsync(
            new AiDraftRequest("student-question", masked.Text, sources, warnings),
            cancellationToken);

        return CreateResponse("student-question", draft, sources, warnings, masked.Text);
    }

    public async Task<AssistResponse> CreateExerciseAsync(
        ExerciseAssistRequest request,
        CancellationToken cancellationToken)
    {
        EnsureCourseExists(request.CourseId);
        var input = $"""
        Amne: {request.Topic}
        Niva: {request.TargetLevel ?? "Ej angiven"}
        Tidsram i minuter: {request.DurationMinutes}
        """;
        var masked = piiMasker.Mask(input);
        var warnings = BuildWarnings(input, masked);
        var sources = ragSearchService.Search(request.CourseId, $"{request.Topic} {request.TargetLevel}");

        var draft = await aiDraftService.GenerateDraftAsync(
            new AiDraftRequest("exercise", masked.Text, sources, warnings),
            cancellationToken);

        return CreateResponse("exercise", draft, sources, warnings, masked.Text);
    }

    public async Task<AssistResponse> CreateFeedbackAsync(
        FeedbackAssistRequest request,
        CancellationToken cancellationToken)
    {
        EnsureCourseExists(request.CourseId);
        var input = $"""
        Uppgift: {request.AssignmentTitle}
        Kriterier: {request.Rubric ?? "Ej angivna"}

        Studentinlamning:
        {request.StudentSubmission}
        """;
        var masked = piiMasker.Mask(input);
        var warnings = BuildWarnings(input, masked);
        var sources = ragSearchService.Search(request.CourseId, $"{request.AssignmentTitle} {request.Rubric}");

        var draft = await aiDraftService.GenerateDraftAsync(
            new AiDraftRequest("feedback", masked.Text, sources, warnings),
            cancellationToken);

        return CreateResponse("feedback", draft, sources, warnings, masked.Text);
    }

    public async Task<AssistResponse> HandleStudentQuestionAutomationAsync(
        StudentQuestionAutomationRequest request,
        CancellationToken cancellationToken)
    {
        EnsureCourseExists(request.CourseId);
        var input = $"""
        Student: {request.StudentName ?? "Ej angivet"}
        E-post: {request.StudentEmail}
        Fraga:
        {request.Question}
        """;
        var masked = piiMasker.Mask(input, request.StudentName);
        var warnings = BuildWarnings(input, masked)
            .Concat(["Automation skapade endast ett utkast. Larare maste godkanna innan svar skickas."])
            .Distinct()
            .ToList();
        var sources = ragSearchService.Search(request.CourseId, masked.Text);

        var draft = await aiDraftService.GenerateDraftAsync(
            new AiDraftRequest("student-question", masked.Text, sources, warnings),
            cancellationToken);

        return CreateResponse("automation-student-question", draft, sources, warnings, masked.Text);
    }

    private List<string> BuildWarnings(string originalInput, MaskResult masked)
    {
        return promptSafetyService.Analyze(originalInput)
            .Concat(masked.Warnings)
            .Distinct()
            .ToList();
    }

    private static AssistResponse CreateResponse(
        string taskType,
        string draft,
        IReadOnlyList<SourceReference> sources,
        IReadOnlyList<string> warnings,
        string maskedInput)
    {
        return new AssistResponse(
            "AI-forslag - kraver larargranskning",
            taskType,
            draft,
            sources,
            warnings,
            NeedsTeacherReview: true,
            maskedInput);
    }

    private void EnsureCourseExists(string courseId)
    {
        if (repository.GetCourse(courseId) is null)
        {
            throw new ArgumentException($"Kursen '{courseId}' finns inte.");
        }
    }
}
