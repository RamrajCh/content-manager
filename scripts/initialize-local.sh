#!/bin/bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
file="$root/infra/local/.env"
if [[ -e "$file" ]]; then
    printf '%s\n' 'Existing local environment retained. Credentials and key version were not changed.'
    exit 0
fi

umask 077
mongo_password="$(openssl rand -hex 32)"
storage_secret="$(openssl rand -hex 32)"
encryption_key="$(openssl rand -base64 32)"
lookup_key="$(openssl rand -base64 32)"
temporary="$(mktemp "$root/infra/local/.env.tmp.XXXXXX")"
trap 'rm -f -- "$temporary"' EXIT

{
    printf '%s\n' \
        'ASPNETCORE_ENVIRONMENT=Development' \
        'ASPNETCORE_URLS=http://127.0.0.1:5080' \
        'Platform__LocalServices=true' \
        'Platform__Mongo__Database=contentmanager_development' \
        "Platform__Mongo__ConnectionString=mongodb://contentmanager_local:${mongo_password}@127.0.0.1:27018/?authSource=admin" \
        'Platform__Storage__Endpoint=http://127.0.0.1:19000' \
        'Platform__Storage__Bucket=contentmanager-private' \
        'Platform__Storage__AccessKey=contentmanager_local' \
        "Platform__Storage__SecretKey=$storage_secret" \
        'Platform__Mail__Host=127.0.0.1' \
        'Platform__Mail__Port=11025' \
        'Platform__Security__KeyVersion=v1' \
        "Platform__Security__EncryptionKey=$encryption_key" \
        "Platform__Security__LookupKey=$lookup_key" \
        'LOCAL_MONGO_USER=contentmanager_local' \
        "LOCAL_MONGO_PASSWORD=$mongo_password"
} > "$temporary"

# Publish a complete private file without replacing existing credentials.
if ! ln "$temporary" "$file"; then
    printf '%s\n' 'Could not create local environment without overwriting an existing file. Existing credentials were not changed.' >&2
    exit 1
fi
printf '%s\n' 'Generated ignored development credentials and two independent versioned keys with owner-only permissions. Values were not printed.'
