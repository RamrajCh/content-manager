#!/bin/bash

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
local_environment_file="$root/infra/local/.env"
if [[ ! -f "$local_environment_file" ]]; then
    printf '%s\n' 'Missing local environment. Run bash scripts/initialize-local.sh first.' >&2
    return 1
fi

while IFS= read -r line || [[ -n "$line" ]]; do
    line="${line%$'\r'}"
    [[ "$line" =~ ^[[:space:]]*$ || "$line" == \#* ]] && continue
    if [[ "$line" != *=* ]]; then
        printf '%s\n' 'Invalid local environment entry; values omitted.' >&2
        return 1
    fi
    name="${line%%=*}"
    if [[ ! "$name" =~ ^(ASPNETCORE_[A-Z_]+|Platform__[A-Za-z_]+|LOCAL_[A-Z_]+)$ ]]; then
        printf '%s\n' 'Unsupported local environment variable; values omitted.' >&2
        return 1
    fi
    export "$name=${line#*=}"
done < "$local_environment_file"
unset line name
