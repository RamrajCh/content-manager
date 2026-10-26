#!/bin/bash
set -euo pipefail

script_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd -- "$script_directory/.." && pwd)"
bash "$script_directory/initialize-local.sh"
docker compose --env-file "$root/infra/local/.env" \
    -f "$root/infra/local/compose.yaml" \
    up -d --wait --wait-timeout 120 mongo storage mail

for ((attempt = 0; attempt < 30; attempt++)); do
    if status="$(curl --silent --show-error --noproxy '*' --max-time 2 \
        --output /dev/null --write-out '%{http_code}' 'http://127.0.0.1:19000/')"; then
        if [[ "$status" == 403 ]]; then
            printf '%s\n' 'Development services ready. Services bind only to loopback; no external email delivery is configured.'
            exit 0
        fi
        if [[ "$status" == 2?? ]]; then
            printf '%s\n' 'Storage unexpectedly accepts anonymous requests.' >&2
            exit 1
        fi
    fi
    sleep 2
done
printf '%s\n' 'Private storage did not become ready with anonymous access denied.' >&2
exit 1
