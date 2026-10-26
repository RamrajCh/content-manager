#!/bin/bash
set -euo pipefail

script_directory="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
initialize_database=false
no_build=false
owner_arguments=()
offline_modes=0
confirm_offline=false
while [[ $# -gt 0 ]]; do
    case "$1" in
        --initialize-database) initialize_database=true; offline_modes=$((offline_modes + 1)) ;;
        --no-build) no_build=true ;;
        --provision-owner) owner_arguments+=(--provision-owner); offline_modes=$((offline_modes + 1)) ;;
        --recover-owner) owner_arguments+=(--recover-owner); offline_modes=$((offline_modes + 1)) ;;
        --project-admin-audit) owner_arguments+=(--project-admin-audit); offline_modes=$((offline_modes + 1)) ;;
        --reconcile-publishing) owner_arguments+=(--reconcile-publishing); offline_modes=$((offline_modes + 1)) ;;
        --confirm-offline) confirm_offline=true ;;
        --login) owner_arguments+=(--login "${2:?--login needs a value}"); shift ;;
        *)
            printf '%s\n' 'Usage: bash scripts/run-api.sh [--no-build] [--initialize-database | --provision-owner --login <name> | --recover-owner | --project-admin-audit | --reconcile-publishing --confirm-offline]' >&2
            exit 1
            ;;
    esac
    shift
done
if [[ "$offline_modes" -gt 1 ]]; then
    printf '%s\n' 'Choose exactly one offline command.' >&2
    exit 1
fi
if [[ "$confirm_offline" == true ]]; then
    owner_arguments+=(--confirm-offline)
fi

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
