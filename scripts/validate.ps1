param([switch]$PublishArtifacts)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $restoreOutput = & dotnet restore backend\ContentManager.Api.csproj --locked-mode '-p:WarningsAsErrors=NU1901%3BNU1902%3BNU1903%3BNU1904'
    $restoreExitCode = $LASTEXITCODE
    $restoreOutput | Write-Output
    if ($restoreExitCode -ne 0) { throw 'Locked .NET restore failed.' }
    $auditWarnings = @($restoreOutput | Where-Object { $_ -match '\bNU190[05]\b' })
    $auditUnavailable = @($auditWarnings | Where-Object { $_ -match '\bNU1900\b.*\b403\b' }).Count -gt 0
    $unexpectedAuditWarnings = @($auditWarnings | Where-Object {
        $_ -notmatch '\bNU1900\b.*\b403\b' -and $_ -notmatch '\bNU1905\b'
    })
    if ($unexpectedAuditWarnings.Count -gt 0 -or ($auditWarnings.Count -gt 0 -and -not $auditUnavailable)) {
        throw '.NET dependency audit failed for a reason other than the accepted HTTP 403 exception.'
    }
    if ($auditUnavailable) {
        Write-Warning 'NuGet audit HTTP 403 accepted for local validation. Dependency review is INCOMPLETE, not clean. Skipping the unavailable NuGet audit; other checks still run.'
    }
    & dotnet build backend\ContentManager.Api.csproj -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw '.NET build failed.' }
    & npm --prefix frontend run lint
    if ($LASTEXITCODE -ne 0) { throw 'Frontend lint failed.' }
    & npm --prefix frontend run build
    if ($LASTEXITCODE -ne 0) { throw 'Frontend build failed.' }
    & npm --prefix frontend run typecheck
    if ($LASTEXITCODE -ne 0) { throw 'Frontend type check failed.' }
    & npm --prefix frontend audit --audit-level=high
    if ($LASTEXITCODE -ne 0) { throw 'npm dependency audit requires review.' }
    if (-not $auditUnavailable) {
        $auditOutput = & dotnet list backend\ContentManager.Api.csproj package --vulnerable --include-transitive --format json
        if ($LASTEXITCODE -ne 0) { throw '.NET dependency audit failed to execute.' }
        $audit = ($auditOutput -join "`n") | ConvertFrom-Json
        $auditOutput | Write-Output
        if ($audit.problems.Count -gt 0 -or $audit.projects.Count -eq 0) {
            throw '.NET dependency audit returned problems or no projects; review is incomplete.'
        }
        foreach ($project in $audit.projects) {
            foreach ($framework in $project.frameworks) {
                foreach ($package in @($framework.topLevelPackages) + @($framework.transitivePackages)) {
                    if ($package.vulnerabilities.Count -gt 0) { throw '.NET dependency vulnerability requires review.' }
                }
            }
        }
    }
    & powershell -NoProfile -File (Join-Path $PSScriptRoot 'review-secrets.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Source secret-pattern review failed.' }
    if ($auditUnavailable) {
        Write-Warning 'Local validation completed with the accepted NuGet audit HTTP 403 exception. A complete dependency review is still required before release.'
    }
    if ($PublishArtifacts) {
        $id = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
        $destination = Join-Path $root "artifacts\$id"
        New-Item -ItemType Directory -Path $destination | Out-Null
        & dotnet publish backend\ContentManager.Api.csproj -c Release --no-build --no-restore -o "$destination\api"
        if ($LASTEXITCODE -ne 0) { throw 'API publish failed.' }
        Copy-Item frontend\.next -Destination "$destination\web-build" -Recurse
        Copy-Item frontend\package.json,frontend\package-lock.json,frontend\next.config.ts -Destination "$destination\web-build"
        Get-ChildItem -LiteralPath $destination -File -Recurse | Get-FileHash -Algorithm SHA256 |
            Select-Object @{Name='Path'; Expression={ $_.Path.Substring($destination.Length + 1) }},Hash |
            Export-Csv -LiteralPath "$destination\sha256.csv" -NoTypeInformation
        Write-Output "Immutable validation snapshot: artifacts\$id (not a production deployment)."
    }
} finally { Pop-Location }
