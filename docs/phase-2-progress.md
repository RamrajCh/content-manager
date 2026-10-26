# Phase 2 Implementation Evidence

**Recorded:** 2026-10-26.  
**Status:** EX-04 implemented locally with acceptance checks still outstanding. EX-05 and EX-06 have not started. Phase 2 is not complete.

Read alongside the [execution plan](./execution-plan.md), [Phase 1 evidence](./phase-1-progress.md), and [owner setup and recovery instructions](../README.md#administrator-setup-and-recovery).

## Prerequisite evidence

The owner reported `npm run validate` passed and authorized implementation. Later backend restores again reported NU1900/NU1905 and HTTP 403 from the NuGet audit feed. Compilation succeeded, but a current clean dependency audit cannot be claimed.

## Implemented boundaries

- Offline owner provisioning and credential recovery; no public registration or recovery endpoint.
- ASP.NET password hashing with 210,000 iterations; Otp.NET TOTP, encrypted authenticator seeds, hashed single-use recovery codes and TOTP replay rejection.
- Opaque Mongo-backed administrator sessions with 30-minute idle and eight-hour absolute expiry checked by the API.
- Secure, HttpOnly, SameSite=Strict host cookies; cookie-bound CSRF tokens and exact configured-origin checks on login and logout.
- Authentication endpoint rate limiting and account lockout after five failed credential checks.
- Administrator login, session-status and logout endpoints, with a frontend sign-in page and authenticated placeholder.
- Explicit session indexes and offline provision/recovery audit events.
- Singleton owner index and additive migration; concurrent offline provisioning cannot create a second owner with a different login.
- Revision-conditional credential/MFA/lockout updates. Session credential generations invalidate old or racing credentials after offline recovery and recovery-code login.
- Durable aggregate audit intents and idempotent explicit audit projection; a projection failure does not lose the audit intent.
- Shared administrator authorization filter for later route groups; documented `totpOrRecoveryCode` login contract and manual HTTP examples.
- Sign-out errors and session-service failures are displayed rather than silently treated as a successful sign-out.

The browser placeholder is not a product editor. Publishing, settings and product/category lifecycle endpoints remain unimplemented.

## Recorded checks

These are local smoke checks, not the full QA-17 suite or an independent security review.

| Check | Expected | Observed |
|---|---|---|
| Backend compilation | Release build compiles | Passed; NuGet audit warnings remained visible |
| Frontend validation | Lint, strict types and production build pass | Passed after navigation warnings were fixed |
| Anonymous session access | 401 | Passed against the direct API |
| Invalid credentials or missing MFA | Generic 401 | Wrong password, wrong code, unknown login and missing code rejected |
| Login CSRF/origin enforcement | 403 without valid CSRF and exact origin | Missing token, missing origin and foreign origin rejected |
| Login and session lookup | Password plus TOTP establishes a usable session | Passed against the direct API |
| Cookie attributes | Secure, HttpOnly, SameSite=Strict, Path=/; no admin cookie Domain | Observed in direct API response headers |
| TOTP replay | Reused accepted time step rejected | 401 observed |
| Logout enforcement | Invalid CSRF rejected; valid logout revokes session | Direct API returned 403/204 respectively; subsequent session lookup returned 401 |
| Recovery code consumption | First use succeeds; reuse fails | 200 then 401 observed |
| Login throttling | Lockout and endpoint rate limits reject excess requests | 429 observed for both checks |
| Idle and absolute expiry | Expired sessions rejected regardless of TTL cleanup | 401 observed after backdating each timestamp in the local database |
| Offline recovery | Command resets credentials and issues fresh MFA/recovery material | Command exercised locally; subsequent isolated service checks also verified old-generation and racing sessions are rejected |
| Browser origin mismatch | Unconfigured `localhost` origin rejected | Owner reported 403 at `http://localhost:3000` |
| Browser login through frontend proxy | Configured origin signs in and displays owner identity | Owner confirmed "Signed in as owner" at `http://127.0.0.1:3000` |

### Follow-up acceptance run

An ad hoc service/HTTP smoke harness ran against a newly generated isolated Mongo
database, with generated credentials and keys held in memory. It passed **37
checks**, then stopped its API process and deleted only its own QA database. No
owner credentials in the normal development database were reset. This harness
is session-local evidence, not a new required automated repository test suite.

Covered behavior:

- Repeated index initialization and simultaneous different-login owner provisioning.
- Password/TOTP sign-in, TOTP replay, malformed/anonymous tokens, recovery-code single consumption under concurrency, recovery-code generation rotation.
- Five simultaneous invalid-password attempts causing lockout, and expiry of that lockout.
- Disabled-owner login/session rejection and download-scoped capability rejection, including through HTTP.
- Exact idle/absolute expiry boundaries with a controlled service clock, without relying on TTL cleanup.
- Revocation without session resurrection by activity updates; offline recovery racing old-credential logins.
- Old authenticator rejection/new authenticator acceptance after offline recovery.
- Durable audit intents and repeated audit projection without duplicate global events.
- HTTP cookie flags, configured-origin and CSRF rejection, login JSON shape, session lookup, authenticated logout and post-logout 401.
- HTTP rate limiting and absence of supplied secrets/tokens in captured API output.

Additional validation:

| Gate | Latest result |
| --- | --- |
| Backend Release build | Passed, zero compiler errors; NU1900/NU1905 warnings remain in normal builds |
| Frontend lint/typecheck/build | Passed |
| npm audit | Zero reported vulnerabilities |
| Source secret-pattern review | Passed; not a full secret scanner |
| PowerShell launcher syntax/exclusive command guard | Passed |
| Normal development index migration | Passed without resetting owner credentials |
| Rebuilt service startup | API readiness, frontend login page and same-origin readiness proxy returned 200; live Swagger exposes `totpOrRecoveryCode` |
| Full strict validation | **Blocked:** restore now correctly fails on NU1900/NU1905, HTTP 403 from the audit source |

An earlier full-run attempt first encountered the running API executable's build
lock. The services stopped before the approved restart attempt, allowing a
normal rebuild. The strict runner was tightened to make audit-source failures
errors in validation only. A standalone package-list command returned an empty
vulnerability list despite restore warnings; that is not accepted as proof of a
complete audit.

## Outstanding acceptance and follow-up

### Owner-approved local validation exception

After the earlier blocked runs, the owner explicitly requested that the NuGet
audit HTTP 403 no longer block validation. The runner now accepts NU1900 with
HTTP 403 and associated NU1905 as a local-only dependency-review exception,
prints an INCOMPLETE warning, and continues other checks. It does not disable
NuGet auditing, accept known vulnerabilities, or suppress unrelated failures.
The earlier strict-validation results above remain historical evidence.
This changes the local validation gate, not the requirement for complete
dependency review before release. No package is automatically marked Done by
this policy change.

Validation-policy verification passed ten mocked command scenarios: healthy
audit, accepted audit 403, rejected audit 503, missing data without 403, mixed
403/unrelated audit errors, package-restore failure, known vulnerability,
compiler failure, audit problems and vulnerabilities returned by the audit
command. A real locked restore also succeeded with visible NU1900/NU1905 HTTP
403 warnings under the new policy. The full build/frontend runner was not
rerun for this policy-only change; running services were left untouched.

- Browser sign-out and subsequent unauthorized access have not been confirmed by the owner; direct API logout passed.
- Browser failure-message behavior has not been manually exercised; API/service checks do not substitute for browser UX verification.
- Production TLS, least-privilege deployment and the broader QA-17 checks remain launch obligations.
- Bash launcher changes were inspected but not executed on macOS in this Windows session.
- Complete NuGet vulnerability review before release, including Otp.NET; local validation may proceed with the owner-approved HTTP 403 exception.
- Rotate the development credentials issued during smoke checks before using any real data. Credentials disclosed in chat are test-only and are deliberately omitted from this document.
- Authentication review and production TLS/cookie behavior remain outstanding. Local success is not production approval.

Do not mark EX-04 Done or accept EX-05/EX-06 until the relevant prerequisite and acceptance gates are satisfied.
