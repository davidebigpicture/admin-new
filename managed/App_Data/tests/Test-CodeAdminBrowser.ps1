param(
    [string]$BaseUrl = "https://dev.services.wvbps.wv.gov/dev/adminshell/managed/code-admin"
)

$ErrorActionPreference = "Stop"

function Assert-StatusCode {
    param(
        [string]$Label,
        [int]$Expected,
        [int]$Actual
    )

    if ($Actual -ne $Expected) {
        throw "$Label expected HTTP $Expected but received HTTP $Actual."
    }
}

$sessionOutput = curl.exe -sS -w "`nHTTP:%{http_code}" "$BaseUrl/api/session.ashx"
$sessionLines = $sessionOutput -split "`n"
$sessionStatus = [int]($sessionLines[-1].Replace("HTTP:", ""))
$sessionBodyText = ($sessionLines[0..($sessionLines.Length - 2)] -join "`n").Trim()

Assert-StatusCode "unauthenticated session api" 401 $sessionStatus

$body = $sessionBodyText | ConvertFrom-Json
if ($body.ok -ne $false -or $body.error -ne "Authentication is required.") {
    throw "unauthenticated session api did not return the expected auth error payload."
}

$pageOutput = curl.exe -sS -o NUL -w "%{http_code}" "$BaseUrl/index.aspx"
$pageStatus = [int]$pageOutput
if ($pageStatus -ne 302 -and $pageStatus -ne 200) {
    throw "code admin page expected redirect or success but received HTTP $pageStatus."
}

Write-Output "Code Admin browser smoke test passed."
