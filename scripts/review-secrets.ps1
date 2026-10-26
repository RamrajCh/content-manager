$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$files = @(
    Get-ChildItem (Join-Path $root 'backend') -Recurse -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
    Get-ChildItem (Join-Path $root 'frontend\src') -Recurse -File
    Get-Item (Join-Path $root 'frontend\next.config.ts')
    Get-ChildItem (Join-Path $root 'scripts') -File
    Get-Item (Join-Path $root 'infra\local\compose.yaml')
)
$patterns = @(
    'mongodb(?:\+srv)?://[^:\s]+:[^$<\s][^@\s]*@',
    '(?:AKIA|ASIA)[A-Z0-9]{16}',
    '-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----',
    're_[A-Za-z0-9]{20,}'
)
$findings = @()
foreach ($file in $files) {
    $content = [IO.File]::ReadAllText($file.FullName)
    foreach ($pattern in $patterns) {
        if ($content -match $pattern) { $findings += $file.FullName.Substring($root.Length + 1); break }
    }
}
if ($findings.Count -gt 0) {
    $findings | ForEach-Object { Write-Output "Review required: $_ (matching content withheld)" }
    throw 'Potential embedded secrets found. No values printed.'
}
Write-Output 'Source secret-pattern review passed. This narrow local check is not a full secret scanner or independent security review.'
