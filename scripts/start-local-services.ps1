$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot 'initialize-local.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Local environment initialization failed.' }
$compose = Join-Path $root 'infra\local\compose.yaml'
$envFile = Join-Path $root 'infra\local\.env'
& docker compose --env-file $envFile -f $compose up -d --wait --wait-timeout 120 mongo storage mail
if ($LASTEXITCODE -ne 0) { throw 'Local containers failed to start. Check Docker and port availability.' }
$storageReady = $false
for ($attempt = 0; $attempt -lt 30; $attempt++) {
    try {
        $response = Invoke-WebRequest -UseBasicParsing 'http://127.0.0.1:19000/' -TimeoutSec 2
        throw 'Storage unexpectedly accepts anonymous requests.'
    } catch [System.Net.WebException] {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 403) {
            $storageReady = $true
            break
        }
        Start-Sleep -Seconds 2
    }
}
if (-not $storageReady) { throw 'Private storage did not become ready with anonymous access denied.' }
Write-Output 'Development services ready. Services bind only to loopback; no external email delivery is configured.'
