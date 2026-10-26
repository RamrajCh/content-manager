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
| Offline recovery | Command resets credentials and issues fresh MFA/recovery material | Command exercised locally; cross-session revocation race not tested |
| Browser origin mismatch | Unconfigured `localhost` origin rejected | Owner reported 403 at `http://localhost:3000` |
| Browser login through frontend proxy | Configured origin signs in and displays owner identity | Owner confirmed "Signed in as owner" at `http://127.0.0.1:3000` |

## Outstanding acceptance and follow-up

- Browser sign-out and subsequent unauthorized access have not been confirmed by the owner; direct API logout passed.
- Complete the remaining QA-17 cases, including visitor-capability rejection, disabled-owner access, concurrent login/recovery/session revocation and secret-log checks.
- Review owner uniqueness under concurrent offline provisioning, durable audit intent/projection, and authorization reuse before introducing additional administrator endpoints.
- Reconcile the login request's implemented `code` field with the design contract's `totpOrRecoveryCode` field.
- Re-run strict validation with complete NuGet vulnerability data, including the newly added Otp.NET dependency.
- Rotate the development credentials issued during smoke checks before using any real data. Credentials disclosed in chat are test-only and are deliberately omitted from this document.
- Authentication review and production TLS/cookie behavior remain outstanding. Local success is not production approval.

Do not mark EX-04 Done or accept EX-05/EX-06 until the relevant prerequisite and acceptance gates are satisfied.
