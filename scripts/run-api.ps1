param([switch]$InitializeDatabase, [switch]$NoBuild, [switch]$ProvisionOwner, [switch]$RecoverOwner, [switch]$ProjectAdminAudit, [string]$Login)
$ErrorActionPreference = 'Stop'
if (@($InitializeDatabase, $ProvisionOwner, $RecoverOwner, $ProjectAdminAudit).Where({ $_.IsPresent }).Count -gt 1) {
    throw 'Choose exactly one offline command.'
}
. (Join-Path $PSScriptRoot 'import-local-environment.ps1')
$project = Join-Path $root 'backend\ContentManager.Api.csproj'
$arguments = @('run', '--project', $project, '--no-launch-profile', '-c', 'Release')
if ($NoBuild) { $arguments += @('--no-build', '--no-restore') }
if ($InitializeDatabase) { $arguments += @('--', '--initialize-database') }
if ($ProvisionOwner) {
    if (-not $Login) { throw '-Login is required with -ProvisionOwner.' }
    $arguments += @('--', '--provision-owner', '--login', $Login)
}
if ($RecoverOwner) { $arguments += @('--', '--recover-owner') }
if ($ProjectAdminAudit) { $arguments += @('--', '--project-admin-audit') }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'API command failed. Review the safe diagnostics above.' }
