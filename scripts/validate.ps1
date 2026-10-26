param([switch]$PublishArtifacts)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    & dotnet restore backend\ContentManager.Api.csproj --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Locked .NET restore failed.' }
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
    & powershell -NoProfile -File (Join-Path $PSScriptRoot 'review-secrets.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'Source secret-pattern review failed.' }
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
