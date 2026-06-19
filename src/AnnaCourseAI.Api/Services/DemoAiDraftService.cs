using AnnaCourseAI.Api.Models;

namespace AnnaCourseAI.Api.Services;

public sealed class DemoAiDraftService : IAiDraftService
{
    public Task<string> GenerateDraftAsync(AiDraftRequest request, CancellationToken cancellationToken)
    {
        var sourceList = string.Join(
            Environment.NewLine,
            request.Sources.Select((source, index) =>
                $"{index + 1}. {source.Title}: {source.Snippet}"));

        var warnings = request.SafetyWarnings.Count == 0
            ? "Inga varningar."
            : string.Join(" ", request.SafetyWarnings);

        var draft = request.TaskType switch
        {
            "student-question" => BuildStudentAnswer(request.UserInput, sourceList, warnings),
            "exercise" => BuildExercise(request.UserInput, sourceList, warnings),
            "feedback" => BuildFeedback(request.UserInput, sourceList, warnings),
            _ => BuildGenericDraft(request.UserInput, sourceList, warnings)
        };

        return Task.FromResult(draft);
    }

    private static string BuildStudentAnswer(string input, string sources, string warnings)
    {
        return $"""
        AI-forslag till svar:

        Hej!

        Tack for din fraga. Jag kan inte lova godkant eller fatta beslut om sena inlamningar automatiskt. Mitt forslag ar att du kontrollerar kursens instruktioner och kompletteringsinformation i larplattformen. Om datumet fortfarande ar otydligt kan du skriva till lararen och beskriva vilken ovning det galler, sa kan lararen ge ett korrekt besked.

        Kallor som anvandes:
        {sources}

        Lararkontroll:
        Kontrollera datum, kursregel och ton innan svaret skickas. {warnings}

        Obs: detta ar ett forslag, inte ett automatiskt beslut.

        Studentens fraga som behandlades:
        {input}
        """;
    }

    private static string BuildExercise(string input, string sources, string warnings)
    {
        return $"""
        AI-forslag till ovning:

        Titel: Bygg ett granskat AI-stod for kursfeedback

        Syfte:
        Studenterna ska ova pa att koppla en AI-losning till ett verkligt verksamhetsproblem och samtidigt hantera sakerhet, GDPR och manuell granskning.

        Instruktion:
        1. Valj ett litet kursmaterial och lagg in det i systemet.
        2. Skapa en studentfraga eller kort inlamning.
        3. Lat AI-stodet skapa ett forslag.
        4. Markera vad lararen maste kontrollera innan svaret anvands.
        5. Beskriv en risk enligt OWASP LLM Top 10 och en konkret skyddsatgard.

        Tidsram:
        Anpassa till {input}.

        Bedomningsunderlag:
        Studenten ska kunna visa fungerande flode, relevanta kallor och tydlig granskningspunkt.

        Kallor som anvandes:
        {sources}

        Lararkontroll:
        {warnings}
        """;
    }

    private static string BuildFeedback(string input, string sources, string warnings)
    {
        return $"""
        AI-forslag till feedback:

        Styrkor:
        - Texten visar att studenten har forsokt koppla losningen till caset.
        - Det finns en tydlig riktning mot praktisk nytta for lararen.

        Forbattra:
        - Koppla resonemanget tydligare till kursmaterial eller bedomningskriterier.
        - Visa var AI-forslaget maste granskas manuellt.
        - Undvik att lata AI:n fatta beslut om betyg eller godkant.

        Nasta steg:
        Komplettera med ett konkret exempel fran din backend eller automation och forklar vilken risk det hanterar.

        Kallor som anvandes:
        {sources}

        Lararkontroll:
        Detta ar ett feedbackforslag. Anna ska kontrollera ton, korrekthet och om nagot individuellt stod behover laggas till. {warnings}

        Underlag som analyserades:
        {input}
        """;
    }

    private static string BuildGenericDraft(string input, string sources, string warnings)
    {
        return $"""
        AI-forslag:

        Baserat pa kursmaterialet kan detta besvaras genom att koppla fragan till Annas RAG-baserade AI-stod och kravet pa larargranskning.

        Kallor som anvandes:
        {sources}

        Lararkontroll:
        {warnings}

        Underlag:
        {input}
        """;
    }
}
