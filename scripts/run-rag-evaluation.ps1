param(
    [string]$BackendBaseUrl = "http://localhost:51800",
    [string]$DatasetPath = "$PSScriptRoot/../evaluation/rag-answer-cases.json",
    [string]$OutputPath = "$PSScriptRoot/../artifacts/rag-evaluation-report.md"
)

$ErrorActionPreference = "Stop"
$cases = Get-Content -LiteralPath $DatasetPath -Raw | ConvertFrom-Json
$results = @()

foreach ($case in $cases) {
    $body = @{ query = $case.query; maxResults = 8 } | ConvertTo-Json
    try {
        $response = Invoke-RestMethod -Method Post -Uri "$BackendBaseUrl/api/v1/search/answer/debug" -ContentType "application/json; charset=utf-8" -Body ([System.Text.Encoding]::UTF8.GetBytes($body))
        $selectedText = (($response.contextChunksSentToLlm | ForEach-Object { "$($_.contentTitle) $($_.contentUrl)" }) -join " ").ToLowerInvariant()
        $expected = @($case.expectedAny | Where-Object { $selectedText.Contains($_.ToLowerInvariant()) })
        $forbidden = @($case.forbidden | Where-Object { $selectedText.Contains($_.ToLowerInvariant()) })
        $results += [pscustomobject]@{
            Name = $case.name
            Passed = ($expected.Count -gt 0 -and $forbidden.Count -eq 0)
            Sources = @($response.contextChunksSentToLlm).Count
            RetrievalMs = $response.timing.retrievalMilliseconds
            RerankMs = $response.timing.rerankMilliseconds
            AnswerMs = $response.timing.answerMilliseconds
            TotalMs = $response.timing.totalMilliseconds
            Details = if ($forbidden.Count -gt 0) { "Forbidden: $($forbidden -join ', ')" } else { "Expected: $($expected -join ', ')" }
        }
    }
    catch {
        $results += [pscustomobject]@{ Name = $case.name; Passed = $false; Sources = 0; RetrievalMs = 0; RerankMs = 0; AnswerMs = 0; TotalMs = 0; Details = $_.Exception.Message }
    }
}

$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$lines = @("# RAG Evaluation Report", "", "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz')", "", "| Scenario | Result | Sources | Retrieval | Rerank | Answer | Total | Details |", "|---|---:|---:|---:|---:|---:|---:|---|")
foreach ($result in $results) {
    $status = if ($result.Passed) { "PASS" } else { "FAIL" }
    $details = $result.Details.ToString().Replace("|", "\|").Replace("`r", " ").Replace("`n", " ")
    $lines += "| $($result.Name) | $status | $($result.Sources) | $($result.RetrievalMs) ms | $($result.RerankMs) ms | $($result.AnswerMs) ms | $($result.TotalMs) ms | $details |"
}
$passedCount = @($results | Where-Object Passed).Count
$lines += "", "Result: $passedCount/$($results.Count) scenarios passed."
$lines | Set-Content -LiteralPath $OutputPath -Encoding UTF8
$results | Format-Table Name, Passed, Sources, RetrievalMs, RerankMs, AnswerMs, TotalMs
Write-Host "Report: $([System.IO.Path]::GetFullPath($OutputPath))"
if ($passedCount -ne $results.Count) { exit 1 }
