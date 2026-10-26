$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$file = Join-Path $root 'infra\local\.env'
if (Test-Path -LiteralPath $file) {
    Write-Output 'Existing local environment retained. Credentials and key version were not changed.'
    exit 0
}

function New-RandomValue {
    param([switch]$Hex)
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    if ($Hex) { return ([BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant() }
    return [Convert]::ToBase64String($bytes)
}

$mongoPassword = New-RandomValue -Hex
$values = [ordered]@{
    ASPNETCORE_ENVIRONMENT = 'Development'
    ASPNETCORE_URLS = 'http://127.0.0.1:5080'
    Platform__LocalServices = 'true'
    Platform__Mongo__Database = 'contentmanager_development'
    Platform__Mongo__ConnectionString = "mongodb://contentmanager_local:${mongoPassword}@127.0.0.1:27018/?authSource=admin"
    Platform__Storage__Endpoint = 'http://127.0.0.1:19000'
    Platform__Storage__Bucket = 'contentmanager-private'
    Platform__Storage__AccessKey = 'contentmanager_local'
    Platform__Storage__SecretKey = New-RandomValue -Hex
    Platform__Mail__Host = '127.0.0.1'
    Platform__Mail__Port = '11025'
    Platform__Security__KeyVersion = 'v1'
    Platform__Security__EncryptionKey = New-RandomValue
    Platform__Security__LookupKey = New-RandomValue
    LOCAL_MONGO_USER = 'contentmanager_local'
    LOCAL_MONGO_PASSWORD = $mongoPassword
}
$content = ($values.GetEnumerator() | ForEach-Object { '{0}={1}' -f $_.Key, $_.Value }) -join "`n"
$stream = [System.IO.File]::Open($file, [System.IO.FileMode]::CreateNew)
try {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($content + "`n")
    $stream.Write($bytes, 0, $bytes.Length)
} finally { $stream.Dispose() }
if ($env:OS -eq 'Windows_NT') {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
    & icacls.exe $file '/inheritance:r' '/grant:r' "${identity}:(F)" '*S-1-5-18:(F)' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Local environment created, but restricting its ACL failed. Fix access before use.' }
}
Write-Output 'Generated ignored development credentials and two independent versioned keys. Values were not printed.'
