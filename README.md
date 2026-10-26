# Content Manager

Phase 1 development foundation for a single-owner digital-download platform.
The frontend and backend are separate applications; publishing, authentication,
commerce and email fulfillment are not implemented yet.

## Structure

| Directory | Responsibility |
|---|---|
| [frontend](./frontend) | Next.js, TypeScript strict mode, Tailwind, ESLint |
| [backend](./backend) | ASP.NET Core 10 API, typed configuration, Mongo readiness and explicit index setup |
| [infra/local](./infra/local) | Loopback-only MongoDB, authenticated SeaweedFS S3 and Mailpit capture containers |
| [scripts](./scripts) | Local settings, service/API startup and validation |
| [docs](./docs) | [Design index](./docs/technical-design-document.md), [execution plan](./docs/execution-plan.md) and [Phase 1 evidence](./docs/phase-1-progress.md) |

## Prerequisites

- Node.js/npm compatible with [frontend/package.json](./frontend/package.json).
  Validated locally with Node 26.11.1 and npm 11.20.0; [.node-version](./.node-version)
  records the runtime used.
- .NET SDK 10.0.402; [global.json](./global.json) allows its later patch releases.
- Docker Desktop using Linux containers, Compose v2, and a running engine.
- Windows: PowerShell 5.1 or compatible PowerShell.
- macOS: Bash 3.2 or later, OpenSSL and curl, plus the same Node/.NET/Docker
  prerequisites above. Invoke the scripts with `bash`, including from zsh;
  executable permission changes are not required.
- Commands below run from the workspace root in separate terminals where noted.

No Git repository, cloud deployment, provider account or paid subscription is
created by these commands. [NuGet.Config](./NuGet.Config) scopes package/audit
sources to public nuget.org without changing machine-wide configuration.

OneDrive read-only flags on generated dependency directories caused `EBUSY`
during installation in this workspace. Clearing those flags only in generated
dependencies allowed a clean `npm ci`; no global sync settings were changed.
If it recurs, consider an owner-approved non-synced development location.

## First startup

### Windows PowerShell

```powershell
npm --prefix frontend ci
dotnet restore .\backend\ContentManager.Api.csproj --locked-mode
powershell -NoProfile -File .\scripts\start-local-services.ps1
powershell -NoProfile -File .\scripts\run-api.ps1 -InitializeDatabase
```

Index initialization is explicit and idempotent, not an HTTP/admin endpoint.
Phase 1 sets up only the product/slug/request/reference uniqueness indexes;
later feature phases add their own indexes and model validation.

Run the API in one terminal:

```powershell
powershell -NoProfile -File .\scripts\run-api.ps1
```

Run the frontend in another terminal:

```powershell
npm run dev:web
```

### macOS Terminal

Start Docker Desktop before running these commands. The service script creates
local credentials if missing, starts containers and polls only the local S3
endpoint for readiness. It does not install tools or change system settings.

```bash
npm --prefix frontend ci
dotnet restore ./backend/ContentManager.Api.csproj --locked-mode
bash ./scripts/start-local-services.sh
bash ./scripts/run-api.sh --initialize-database
```

Run the API in one terminal:

```bash
bash ./scripts/run-api.sh
```

Run the frontend in another terminal:

```bash
npm run dev:web
```

The Bash scripts resolve paths relative to their own location and support
workspace paths containing spaces. They target local Development only, using
the same environment names, ports and Compose volumes as the PowerShell scripts.
The macOS scripts have not been executed or validated in this Windows session.

### Local endpoints

| Endpoint | Expected result |
|---|---|
| `http://127.0.0.1:3000` | Development foundation page |
| `http://127.0.0.1:3000/api/v1/health/ready` | Same-origin proxy to API |
| `http://127.0.0.1:5080/api/v1/health/live` | 200 when process is live |
| `http://127.0.0.1:5080/api/v1/health/ready` | 200 when Mongo responds; 503 during DB outage |
| `http://127.0.0.1:19000` | S3 endpoint; anonymous requests denied |
| `http://127.0.0.1:18025` | Mailpit UI; captured local messages only |

Mongo binds to 127.0.0.1:27018; SMTP capture binds to 127.0.0.1:11025.
Do not expose these development services to a LAN or public host. The readiness
endpoint checks Mongo, not future object/email integrations. Startup script
separately verifies S3 availability with anonymous access denied.

## Secrets and persistence

[initialize-local.ps1](./scripts/initialize-local.ps1) creates an ignored local
environment file with random credentials and two independent base64 32-byte keys
under version `v1`. It retains existing values on subsequent runs and restricts
the file's Windows ACL. No secret values are printed. The template
[.env.example](./infra/local/.env.example) contains names, not usable credentials.

On macOS, [initialize-local.sh](./scripts/initialize-local.sh) generates the same
settings with OpenSSL and owner-only file permissions (`0600`). It publishes
the completed file without overwriting an existing environment. The API script
imports allowlisted entries as literal values, not executable shell code.
Do not run the scripts with shell tracing (`bash -x`), which can expose secrets.
Generate credentials separately on each machine instead of copying the ignored
environment file between Windows and macOS.

Named Docker volumes retain Mongo/object/mail data. Local Mongo uses a generated
root account for development bootstrap only, not a production least-privilege
account. The S3 credentials are development credentials only. Key loading is
validated now; application encryption, key rotation/multi-version decryption,
sessions and business services belong to later phases. Do not treat local
configuration as a production secret store or backup.

Stop containers without deleting volumes:

```powershell
docker compose --env-file .\infra\local\.env -f .\infra\local\compose.yaml stop
```

macOS:

```bash
docker compose --env-file ./infra/local/.env -f ./infra/local/compose.yaml stop
```

Do not use volume deletion as a restart command. Deleting/replacing the local
environment file while retaining authenticated data can invalidate access to
that data. Back up keys/data and plan credential rotation together.

## Validation

The validation runner remains PowerShell-based; this change adds macOS setup and
startup scripts, not a separate Bash validation pipeline.

```powershell
npm run validate
```

Runs locked .NET restore, Release build, frontend lint/build/type
checks, dependency audits and a narrow source-secret-pattern check. No automated
unit/integration test suite is required or introduced.

The API project does not treat warnings as errors. NuGet auditing remains
enabled, and audit-feed failures such as NU1900/NU1905 are visible warnings
that do not block compilation or normal API startup:

```powershell
dotnet restore .\backend\ContentManager.Api.csproj --locked-mode
dotnet build .\backend\ContentManager.Api.csproj -c Release --no-restore
powershell -NoProfile -File .\scripts\run-api.ps1
```

macOS equivalent:

```bash
dotnet restore ./backend/ContentManager.Api.csproj --locked-mode
dotnet build ./backend/ContentManager.Api.csproj -c Release --no-restore
bash ./scripts/run-api.sh
```

This policy applies to all build warnings, including known vulnerability
warnings; review them rather than interpreting a successful build as a clean
dependency audit. The audit update resource has returned HTTP 403 locally.
At the owner's request, local validation accepts the known NuGet audit HTTP 403
as a non-blocking exception. If restore reports NU1900 with HTTP 403 (and its
associated NU1905), the runner warns that dependency review is **INCOMPLETE**
and skips the unavailable NuGet package audit. Build, frontend checks, npm audit
and source-secret review still run. NuGet auditing stays enabled; when the feed
is available, the normal audit runs automatically. Other audit failures and
reported vulnerabilities still fail validation; NU1901 through NU1904 are
treated as errors during validation restore.

A successful local run with this exception is not a clean dependency review or
production approval. Complete the NuGet review before release; the exception
allows local validation to proceed without waiting for the audit feed.

Frontend clean install, lint, build, typecheck and npm audit passed. ESLint
9.39.5 is deprecated upstream but retained for the scaffold-compatible peer
graph; an ESLint 10 trial was incompatible. A supported compatible upgrade
remains a maintenance follow-up, separate from the NuGet audit blocker.

After strict validation passes, optionally create a uniquely named local
snapshot of the built outputs and SHA-256 manifest:

```powershell
powershell -NoProfile -File .\scripts\validate.ps1 -PublishArtifacts
```

Snapshots are ignored build evidence, not deployable production bundles or an
activated CI pipeline. Frontend runtime dependencies and environment-specific
ingress/deployment packaging are still required. Do not edit or reuse an
existing snapshot directory as a new release. CI provider activation, external
alert delivery, staging/production secrets and hosting remain EX-27 obligations.

## Administrator setup and recovery

EX-04 authentication is implemented locally; publishing tools are not available
yet. See [Phase 2 evidence](docs/phase-2-progress.md) for checks and outstanding
acceptance gates.

Use **http://127.0.0.1:3000/admin/login**, not `localhost`. The development API
expects the exact origin configured by `Platform:Security:PublicOrigin` in
[development settings](backend/appsettings.Development.json). A different
hostname is a different origin and login is rejected with 403 before checking
credentials. The production origin must be configured explicitly and use HTTPS.
Cookies remain Secure in development; if a browser refuses them over local HTTP,
use a local HTTPS setup rather than weakening the cookie policy.

Run index setup before first provisioning. In an interactive PowerShell terminal:

```powershell
powershell -NoProfile -File .\scripts\run-api.ps1 -InitializeDatabase
powershell -NoProfile -File .\scripts\run-api.ps1 -ProvisionOwner -Login owner
```

macOS equivalent:

```bash
bash ./scripts/run-api.sh --initialize-database
bash ./scripts/run-api.sh --provision-owner --login owner
```

Also rerun index setup when upgrading an existing local database. It adds the
singleton owner slot without changing credentials. Multiple existing owner
records cause initialization to fail for offline resolution, not silent deletion.
Choose only one offline operation per invocation.

The command prompts for a new password twice without displaying it (14 to 256
characters). It displays the authenticator secret/URI and eight single-use
recovery codes once. Enroll the secret in a TOTP authenticator (six digits,
30-second period) and store recovery codes offline. Do not capture this output in
logs, screenshots, source files or shared terminals. No default password ships
with the application.

If the owner already exists or access is lost, use offline recovery from a trusted
interactive terminal with access to the correct database and encryption keys:

```powershell
powershell -NoProfile -File .\scripts\run-api.ps1 -RecoverOwner
```

```bash
bash ./scripts/run-api.sh --recover-owner
```

Recovery changes the password, replaces the authenticator secret and recovery
codes, clears lockout and invalidates existing administrator session generations
before deleting old session records. Racing logins using old credentials cannot
produce usable sessions after recovery. Enroll the new
secret and replace the old offline recovery copy. Protect database/CLI access as
administrator-equivalent access. Never use chat-issued smoke-test credentials
with real data.

On the sign-in page, enter either the current authenticator code or an unused
recovery code. A recovery code is consumed when accepted; an accepted TOTP time
step cannot be reused. Recovery-code login also invalidates older administrator
sessions. Five failed credential checks lock the account for
15 minutes, and authentication endpoints also have a per-process IP rate limit.
Sessions expire after 30 minutes of inactivity or eight hours absolutely.

Provisioning/recovery audit intents are saved atomically with the owner change
and projected by event ID. Recovery-code usage also leaves a durable intent.
Retry the idempotent projection (including after a failed projection) with:

```powershell
powershell -NoProfile -File .\scripts\run-api.ps1 -ProjectAdminAudit
```

```bash
bash ./scripts/run-api.sh --project-admin-audit
```

An audit projection error is reported explicitly; it does not undo a completed
credential change. If setup output was interrupted, recover credentials again
from the trusted offline terminal. No automatic background audit projector is
implemented yet.

Manual API examples are in [Admin.http](backend/Admin.http). The login JSON
field is `totpOrRecoveryCode`, matching the design contract. API clients must
retain cookies and fetch a new CSRF token after login. All future administrator
route groups must use the shared `RequireAdministrator` endpoint filter.

## Safety and scope

- Local-service configuration fails outside Development. No production mail
  transport or payment simulator is exposed.
- Configuration validates Mongo, storage/mail settings and key formats without
  echoing their values. HTTP errors use Problem Details with safe codes/trace IDs.
- JSON request logs record method/status/timing/trace ID, not raw paths, query
  strings, headers, bodies, capabilities or credentials. Exception logs record
  type only; default exception diagnostics are suppressed.
- The local S3 implementation is SeaweedFS: MinIO registry images were unavailable
  during setup. This does not change the production S3-compatible/R2 design.
- Container images are pinned by inspected registry digests in Compose.
- No public publishing, payments, download grants, users or provider integrations
  should be assumed from a successful health check.
