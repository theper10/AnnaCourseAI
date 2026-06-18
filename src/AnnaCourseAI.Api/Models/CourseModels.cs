namespace AnnaCourseAI.Api.Models;

public sealed record Course(
    string Id,
    string Name,
    string Description);

public sealed record CourseMaterial(
    string Id,
    string CourseId,
    string Title,
    string Content,
    string SourceType,
    DateTimeOffset UpdatedAt);

public sealed record AddMaterialRequest(
    string Title,
    string Content,
    string? SourceType);

public sealed record SourceReference(
    string MaterialId,
    string Title,
    string Snippet,
    int Score);
