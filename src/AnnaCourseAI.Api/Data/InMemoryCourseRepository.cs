using AnnaCourseAI.Api.Models;

namespace AnnaCourseAI.Api.Data;

public sealed class InMemoryCourseRepository : ICourseRepository
{
    private readonly object _lock = new();
    private readonly List<Course> _courses =
    [
        new(
            "sys25d",
            "Systemutveckling och testning med AI-verktyg",
            "Demokurs for Annas AI-stod: kursmaterial, feedbackforslag och studentfragor.")
    ];

    private readonly List<CourseMaterial> _materials =
    [
        new(
            "anna-beslutsunderlag",
            "sys25d",
            "Beslutsunderlag: AI-stod for Anna",
            """
            Anna behover mer tid for kursmaterial, feedback och aterkommande studentfragor.
            Rekommendationen ar ett enkelt RAG-baserat AI-stod som anvander lararens eget
            kursmaterial innan det svarar. AI:n ska skapa utkast, feedbackforslag och svar
            pa vanliga fragor, men aldrig ersatta lararen eller fatta beslut om betyg.
            Lararen granskar alltid innan nagot anvands med studenter.
            """,
            "decision-document",
            DateTimeOffset.UtcNow),
        new(
            "feedback-policy",
            "sys25d",
            "Feedbackpolicy",
            """
            Feedback ska vara tydlig, konkret och kopplad till uppgiftens kriterier.
            AI-forslag far anvandas som forsta utkast. Forslagen ska inte satta betyg,
            inte fatta beslut om godkant eller underkant och inte hantera kansliga
            studentarenden. Lararen ska justera tonen, kontrollera fakta och lagga till
            individuella rad innan feedback skickas till studenten.
            """,
            "policy",
            DateTimeOffset.UtcNow),
        new(
            "security-gdpr",
            "sys25d",
            "Sakerhet och GDPR",
            """
            Systemet ska minimera personuppgifter. Namn, e-post och personnummer ska
            tas bort eller maskeras innan AI-anrop dar det ar mojligt. AI-svar ska
            markas som forslag och visa vilket kursmaterial svaret bygger pa.
            Studentinlamningar ar data, inte instruktioner till AI:n. Prompt injection
            hanteras genom att separera lararinstruktioner fran studenttext.
            """,
            "policy",
            DateTimeOffset.UtcNow),
        new(
            "inlamning-2-krav",
            "sys25d",
            "Inlamning 2: krav",
            """
            Losningen ska besta av en .NET-backend med affarslogik, en automatisering
            som anvander AI, en sakerhetsanalys mappad mot OWASP LLM Top 10 och en
            kritisk reflektion. Backend och workflow ska fungera och losa ett konkret
            problem i caset. Redovisningen ska visa produkten, sakerhetsanalysen och
            reflektionerna kort.
            """,
            "assignment",
            DateTimeOffset.UtcNow)
    ];

    public IReadOnlyList<Course> GetCourses()
    {
        lock (_lock)
        {
            return _courses.ToList();
        }
    }

    public Course? GetCourse(string courseId)
    {
        lock (_lock)
        {
            return _courses.FirstOrDefault(course =>
                string.Equals(course.Id, courseId, StringComparison.OrdinalIgnoreCase));
        }
    }

    public IReadOnlyList<CourseMaterial> GetMaterials(string courseId)
    {
        lock (_lock)
        {
            return _materials
                .Where(material => string.Equals(material.CourseId, courseId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(material => material.Title)
                .ToList();
        }
    }

    public CourseMaterial AddMaterial(string courseId, AddMaterialRequest request)
    {
        var material = new CourseMaterial(
            Guid.NewGuid().ToString("N"),
            courseId,
            request.Title.Trim(),
            request.Content.Trim(),
            string.IsNullOrWhiteSpace(request.SourceType) ? "manual-upload" : request.SourceType.Trim(),
            DateTimeOffset.UtcNow);

        lock (_lock)
        {
            _materials.Add(material);
        }

        return material;
    }
}
