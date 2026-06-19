# AnnaCourseAI

AnnaCourseAI är en liten .NET-backend och automation för caset i Inlämning 1: ett AI-stöd som hjälper läraren Anna med kursmaterial, feedbackförslag och återkommande studentfrågor.

Lösningen följer beslutsunderlaget: den använder ett enkelt RAG-upplägg, markerar alla svar som förslag, maskerar enkel persondata, varnar för prompt injection och kräver att läraren granskar allt innan studenten ser det.

## Innehåll

- `src/AnnaCourseAI.Api` - .NET 8 Minimal API med affärslogik.
- `automation/n8n-anna-course-ai.workflow.json` - n8n-export för automationen.
- `automation/run-demo-automation.ps1` - körbar demoautomation utan n8n.
- `automation/student-question-demo.json` - exempel på inkommande studentfråga.
- `docs/Inlamning-2-Theodore-Perlman.pdf` - säkerhetsanalys och kritisk reflektion.

## Krav

- .NET SDK 8 eller nyare
- PowerShell
- n8n är valfritt, eftersom samma automation även finns som PowerShell-script

## Kör backend

```powershell
dotnet run --project .\src\AnnaCourseAI.Api --urls http://localhost:5088
```

Testa att API:t svarar:

```powershell
Invoke-RestMethod http://localhost:5088/api/health
```

## Snabbaste sättet att testa allt

Det här scriptet startar backend, väntar tills den är redo, kör demoautomationen och stänger sedan backend igen:

```powershell
powershell -ExecutionPolicy Bypass -File .\run-local-demo.ps1
```

## Kör demoautomation

Starta backend enligt ovan. Kör sedan:

```powershell
powershell -ExecutionPolicy Bypass -File .\automation\run-demo-automation.ps1
```

Automationen skickar en studentfråga till endpointen `/api/automation/student-question`. Backend söker i kursmaterialet, maskerar e-post/persondata, upptäcker prompt injection och returnerar ett AI-förslag som kräver lärargranskning.

## Importera n8n-flöde

1. Öppna n8n.
2. Importera `automation/n8n-anna-course-ai.workflow.json`.
3. Kontrollera URL:en i noden `Call AnnaCourseAI backend`.
   - Om n8n körs i Docker: `http://host.docker.internal:5088`.
   - Om n8n körs direkt på datorn: `http://localhost:5088`.
4. Kör flödet manuellt.

## Använd riktig AI

Utan API-nyckel kör backend i demo-läge med deterministiska svar. För att använda OpenAI-kompatibel chat completion:

```powershell
$env:AI_PROVIDER="OpenAI"
$env:OPENAI_API_KEY="din-api-nyckel"
$env:OPENAI_MODEL="gpt-4o-mini"
dotnet run --project .\src\AnnaCourseAI.Api --urls http://localhost:5088
```

Valfritt kan API:t skyddas med en enkel API-nyckel:

```powershell
$env:ANNA_API_KEY="valfri-hemlighet"
```

Skicka då headern `X-Api-Key` i automationen eller API-testet.

## Viktiga endpoints

- `GET /api/courses` - listar kurser.
- `GET /api/courses/{courseId}/materials` - listar kursmaterial.
- `POST /api/courses/{courseId}/materials` - lägger till material.
- `POST /api/assist/question` - skapar förslag på svar till studentfråga.
- `POST /api/assist/exercise` - skapar övningsutkast.
- `POST /api/assist/feedback` - skapar feedbackförslag på studentinlämning.
- `POST /api/automation/student-question` - automationens endpoint.

## Exempel: feedbackförslag

```powershell
$body = @{
  courseId = "sys25d"
  assignmentTitle = "Inlämning 2"
  rubric = "Backend, automation, OWASP LLM Top 10 och kritisk reflektion"
  studentSubmission = "Jag har byggt ett API och en automation men behöver koppla riskerna tydligare."
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5088/api/assist/feedback" `
  -ContentType "application/json" `
  -Body $body
```

## Designval

Backenden är avsiktligt liten för att vara lätt att granska i en kursinlämning. RAG-delen är en enkel sökning mot inläst kursmaterial i minnet, men gränserna är tydliga: kursmaterial, AI-tjänst och säkerhet ligger i separata tjänster. Det gör att lösningen senare kan byta till databas, vektordatabas eller annan AI-leverantör utan att hela API:t behöver skrivas om.

AI:n får inte publicera, betygsätta eller skicka svar automatiskt. Systemet returnerar alltid `needsTeacherReview: true`.
