# Phase 1 Implementation Evidence

**Recorded:** 2026-10-26.  
**Status:** Local foundation implemented. The owner reported `npm run validate` passed before authorizing Phase 2. Subsequent Phase 2 restores again returned NuGet audit HTTP 403 warnings, so a current complete dependency review remains outstanding. This is not production readiness.

Read alongside the [execution plan](./execution-plan.md) and [local setup guide](../README.md). No Git repository, cloud deployment, live payment integration or external email delivery was created.

## Package status

| Package | Implementation | Acceptance |
|---|---|---|
| EX-01 | Separate top-level [frontend](../frontend) and [backend](../backend), runtime pins, dependency locks, strict TypeScript, ignore rules and startup commands | Clean frontend reinstall, lint/build/types and backend compilation exercised. Backend compilation requires an explicitly documented diagnostic warning override while the audit feed fails. Strict reproducibility gate is not fully accepted |
| EX-02 | Isolated persistent local services, validated configuration/keys, health/errors/traces and explicit idempotent indexes | Local manual checks below passed. This does not implement production adapters or final feature-level schemas |
| EX-03 | Repeatable fail-closed validation, narrow source-pattern review, redacted logs, environment documentation and optional build-snapshot command | **Blocked:** official NuGet audit update returns HTTP 403. Strict validation fails rather than declaring dependencies clean. Snapshot generation has not passed its prerequisite gate |

The tables below preserve the original Phase 1 execution evidence. The later owner-reported validation pass is not a captured command transcript and does not establish that the subsequent audit failure is resolved. EX-04 authentication is now in progress; see [Phase 2 progress](./phase-2-progress.md). EX-05 through EX-30 have not started. No publishing, catalog, payments, download grants, transactional email worker or business analytics are implemented. Automated unit/integration tests were not introduced.

## Implemented boundaries

- Next.js 16.4.3, React 19.3.0, strict TypeScript, Tailwind and scaffold-compatible ESLint 9.39.5. A non-indexable development page links to the same-origin API readiness route.
- ASP.NET Core 10 with MongoDB.Driver 3.13.0, validated typed configuration, safe Problem Details, request trace IDs and JSON logs limited to method/status/timing/trace.
- MongoDB, authenticated SeaweedFS S3 and Mailpit run in the `contentmanager-local` Compose project with persistent named volumes and loopback-only published ports. Images are pinned by registry digests.
- SeaweedFS replaces unavailable MinIO images for local development only; the production private S3-compatible/R2 design is unchanged.
- Local configuration contains generated credentials and two distinct 32-byte base64 keys under version `v1`. An ignored local file has restricted Windows permissions. Key loading is implemented, not application encryption or key rotation.
- Mongo indexes cover product slug, slug reservations, request keys and partial payment-reference uniqueness. Index bootstrap is an explicit command, not a public API.
- CI activation, staging/production deployment, external alerting, real email/storage integrations, backup/restore drills and production security review remain later-phase obligations.

## Manual evidence

These are development smoke checks, not a full QA-01 through QA-23 regression or independent security review.

| Check | Expected result | Observed result |
|---|---|---|
| Web and API startup | Local page and health routes respond | Web 200; API live/ready 200; frontend same-origin readiness 200 |
| Mongo outage | Readiness fails; liveness remains available | Ready 503 and live 200 while Mongo stopped; ready recovered to 200 after restart |
| Mongo persistence | Stored fixture survives service restart | Fixture read back after restart, then removed |
| Private S3 | Signed write/read works; anonymous object read denied | Authenticated PUT/GET passed; anonymous object GET 403 |
| Object persistence | Stored object survives restart | Exact fixture retrieved after readiness polling, then deleted |
| Captured email persistence | Local message survives restart without sending real email | Mailpit fixture using `example.invalid` persisted, then exact message removed |
| Key/settings stability | Reinitialization does not rotate keys unexpectedly | Existing local-file SHA-256 unchanged |
| Missing required key | Startup fails without printing secret values | Configuration failure observed |
| Environment separation | Development services cannot run as production configuration | Production startup rejected; non-development loopback guard included in final compiled build |
| Safe errors | Unknown route returns generic Problem Details and trace | 404 included safe code/trace ID, without configuration or exception details |
| Log redaction | Path/query/header sentinels absent from logs | Synthetic sentinels absent from final JSON request logs; implicit logging scopes disabled |
| Index initialization | Explicit command is repeatable | Two runs succeeded without duplicate-index errors |
| Source secret-pattern check | Clean source passes; synthetic secret fixture fails without echoing contents | Both outcomes observed; synthetic fixture removed; clean check rerun passed |
| Script/Compose syntax | Checked-in operational definitions parse | PowerShell parser and `docker compose config --quiet` passed |
| Clean frontend install | Lockfile reproduces a usable dependency tree | `npm --prefix frontend ci` succeeded after clearing generated-directory read-only flags; 367 packages installed |
| Frontend validation | Lint, production build and strict types pass after clean install | All three commands exited zero; `npm ls eslint --all` reported a valid ESLint 9 graph |
| npm dependency audit | No known reported vulnerabilities | Clean install audit and explicit audit both reported zero vulnerabilities |
| Backend final compilation | Final source compiles while audit failure remains visible | Release build succeeded, zero compiler errors and two NuGet warnings, using temporary `TreatWarningsAsErrors=false` for restore and build |
| Strict validation retry | Any incomplete dependency review stops the gate | Failed on NU1900/NU1905, HTTP 403; no permanent policy relaxation |

Service restart checks verify local persistence, not backup recovery. Storage readiness needs polling; a fixed short delay was insufficient after restart.

## Outstanding checks and maintenance

1. **NuGet vulnerability feed:** public nuget.org package/audit sources are explicitly configured without changing machine settings. The official vulnerability index/base were accessible, but its referenced update returned HTTP 403. No clean .NET dependency review can be claimed. Retry `npm run validate` when the resource is accessible; investigate network/provider policy if it persists. Do not disable auditing to satisfy the gate.
2. **OneDrive filesystem caveat (resolved locally):** an initial `npm ci` failed on `EBUSY` directory removal; a subsequent install failed on rename. Read-only flags were cleared only on generated dependency directories, then locked reinstall, lint/build/typecheck and audit passed. No global sync settings were changed. Prefer a non-synced development location if the issue recurs; relocation is an owner decision.
3. **ESLint maintenance:** ESLint 9.39.5 is deprecated upstream. ESLint 10 was not retained because the scaffold's transitive peer graph was incompatible. The compatible graph previously linted and npm audit reported zero vulnerabilities; deprecation is not proof of a vulnerability. Revisit a supported compatible upgrade, without hiding peer failures.
4. **Artifacts:** optional timestamped local snapshots and SHA-256 manifests are implemented but not yet exercised past strict validation. They are not production bundles or an activated CI pipeline.
5. **Phase gate:** keep EX-03 and Phase 1 acceptance open until strict validation and reproducible startup pass without diagnostic overrides. Production provider adoption/Nepal eligibility and all launch gates remain separate.

## Next actions

Restore official NuGet vulnerability access, rerun strict validation and exercise the optional artifact snapshot if it will be used. Only then accept the Phase 1 gate and begin EX-04 owner authentication. Application servers used for smoke checks have been stopped; approved local Docker services remain running.
