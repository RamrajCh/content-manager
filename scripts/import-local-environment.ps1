$root = Split-Path -Parent $PSScriptRoot
$localEnvironmentFile = Join-Path $root 'infra\local\.env'
if (-not (Test-Path -LiteralPath $localEnvironmentFile)) {
    throw 'Missing local environment. Run scripts\initialize-local.ps1 first.'
}
foreach ($line in [System.IO.File]::ReadAllLines($localEnvironmentFile)) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
    $separator = $line.IndexOf('=')
    if ($separator -lt 1) { throw 'Invalid local environment entry; values omitted.' }
    $name = $line.Substring(0, $separator)
    if ($name -notmatch '^(ASPNETCORE_[A-Z_]+|Platform__[A-Za-z_]+|LOCAL_[A-Z_]+)$') {
        throw 'Unsupported local environment variable; values omitted.'
    }
    [Environment]::SetEnvironmentVariable($name, $line.Substring($separator + 1), 'Process')
}
