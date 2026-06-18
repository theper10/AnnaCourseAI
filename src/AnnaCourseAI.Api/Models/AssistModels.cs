namespace AnnaCourseAI.Api.Models;

public sealed record QuestionAssistRequest(
    string CourseId,
    string Question);

public sealed record ExerciseAssistRequest(
    string CourseId,
    string Topic,
    string? TargetLevel,
    int DurationMinutes);

public sealed record FeedbackAssistRequest(
    string CourseId,
    string AssignmentTitle,
    string StudentSubmission,
    string? Rubric);

public sealed record StudentQuestionAutomationRequest(
    string CourseId,
    string StudentEmail,
    string Question,
    string? StudentName);

public sealed record AssistResponse(
    string Status,
    string TaskType,
    string Draft,
    IReadOnlyList<SourceReference> Sources,
    IReadOnlyList<string> Warnings,
    bool NeedsTeacherReview,
    string MaskedInput);

public sealed record AiDraftRequest(
    string TaskType,
    string UserInput,
    IReadOnlyList<SourceReference> Sources,
    IReadOnlyList<string> SafetyWarnings);
