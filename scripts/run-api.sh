#!/bin/bash
set -euo pipefail

script_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
initialize_database=false
no_build=false
owner_arguments=()
while [[ $# -gt 0 ]]; do
    case "$1" in
        --initialize-database) initialize_database=true ;;
        --no-build) no_build=true ;;
        --provision-owner) owner_arguments=(--provision-owner) ;;
        --recover-owner) owner_arguments=(--recover-owner) ;;
        --login) owner_arguments+=(--login "${2:?--login needs a value}"); shift ;;
        *)
            printf '%s\n' 'Usage: bash scripts/run-api.sh [--initialize-database] [--no-build] [--provision-owner --login <name> | --recover-owner]' >&2
            exit 1
            ;;
    esac
    shift
done

source "$script_directory/import-local-environment.sh"
arguments=(run --project "$root/backend/ContentManager.Api.csproj" --no-launch-profile -c Release)
if [[ "$no_build" == true ]]; then
    arguments+=(--no-build --no-restore)
fi
if [[ "$initialize_database" == true ]]; then
    arguments+=(-- --initialize-database)
fi
if [[ ${#owner_arguments[@]} -gt 0 ]]; then
    arguments+=(-- "${owner_arguments[@]}")
fi
exec dotnet "${arguments[@]}"
