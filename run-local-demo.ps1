param(
    [string]$BaseUrl = "http://localhost:5088"
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "src\AnnaCourseAI.Api"
$outLogPath = Join-Path $env:TEMP "anna-course-ai-demo.out.log"
$errLogPath = Join-Path $env:TEMP "anna-course-ai-demo.err.log"

"Starting AnnaCourseAI backend on $BaseUrl ..."
$server = Start-Process `
    -FilePath "dotnet" `
    -ArgumentList @("run", "--project", $projectPath, "--urls", $BaseUrl) `
    -PassThru `
    -WindowStyle Hidden `
    -RedirectStandardOutput $outLogPath `
    -RedirectStandardError $errLogPath

try {
    $ready = $false
    for ($i = 0; $i -lt 40; $i++) {
        Start-Sleep -Milliseconds 500
        try {
            Invoke-RestMethod "$BaseUrl/api/health" -TimeoutSec 2 | Out-Null
            $ready = $true
            break
        }
        catch {
            # Keep waiting until the backend has finished starting.
        }
    }

    if (-not $ready) {
        throw "Backend did not start. See logs: $outLogPath and $errLogPath"
    }

    "Backend is running."
    ""
    "Running demo automation..."
    & (Join-Path $PSScriptRoot "automation\run-demo-automation.ps1") -BaseUrl $BaseUrl
}
finally {
    if ($server -and -not $server.HasExited) {
        Stop-Process -Id $server.Id -Force
        ""
        "Backend stopped."
    }
}
