# Phase 2 Implementation Evidence

**Recorded:** 2026-10-27.
**Status:** EX-06 is owner-accepted as of 2026-10-27 following owner-reported validation with the expected result. EX-04/EX-05 acceptance evidence and the unrecorded manual scenarios below remain outstanding; this does not declare all Phase 2 gates or production readiness complete.

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

The authentication placeholder has now been extended with the private authoring
workspace described below, including basic settings and an audit browser.
Complete publication remains deliberately blocked until EX-09.

## Publishing implementation continued on 2026-10-27

The existing EX-05 implementation includes private administrator product/category
CRUD, bounded cursor pagination, permanent product slug reservations and redirect
metadata, tags, sanitized descriptions, versioned licenses/pricing, optimistic
revision checks, lifecycle endpoints, durable audit intents/projection and
offline reconciliation. Manual requests are in [Publishing.http](../backend/Publishing.http).
Code presence is not acceptance evidence; QA-01 outcomes are still pending.

The EX-06 authoring UI now provides:

- Product listing/filtering and incremental pagination; create/edit drafts,
  category assignment, tags, description HTML, license and SEO fields.
- A pricing configuration shell for all six modes, using integer NPR paisa.
  Irrelevant pricing fields are cleared when saving a different mode. Zero
  access and optional contributions remain distinct; no payment workflow is
  enabled.
- Explicit revision headers on edits, deletes and lifecycle transitions;
  stable create request keys for unchanged retries; visible API field errors,
  stale-edit instructions, session-expiry redirection and service failures.
- A saved private preview fetched independently of the editor. It renders
  server-sanitized description HTML in an empty-sandbox, network-blocked iframe;
  opening a preview does not advance the editor's revision or overwrite edits.
- Category create/edit/delete, additional category pages, reload controls and
  selected-category preservation while further pages are not yet loaded.
- Archive/unpublish/delete controls with confirmation, unsaved product-change
  prompts and browser-exit warnings. Failed saves preserve entered values.
- A disabled Publish control explaining the EX-09 prerequisite. The API also
  independently rejects publication; the UI is not the enforcement boundary.
- A typed, revision-controlled `standaloneDonationsEnabled` policy, defaulting
  off when unconfigured. Only this allowlisted non-secret setting is returned
  or writable; no arbitrary configuration, receiving-account credentials or
  environment secrets are exposed. A stored opt-in does not implement checkout
  or enable payment collection.
- Durable settings audit intents projected by the existing publishing worker,
  alongside products/categories. The authenticated audit browser exposes only
  event/actor/entity/time/reason/revision metadata, with a default seven-day
  range, maximum 31-day date window and cursor pagination.

Existing attachment references are preserved by metadata edits. Uploads and
attachment replacement are not introduced by this authoring UI.

### Validation ownership and latest attempt

**Owner acceptance recorded 2026-10-27:** The owner explicitly requested
"Accept Ex-06" and reported running validation with the result as expected.
EX-06 is therefore recorded as owner-accepted. This is owner-reported evidence;
the assistant did not rerun validation or inspect its output. No individual
browser/API scenario or clean NuGet vulnerability audit is inferred from this
report. The existing HTTP 403 exception and release obligations still apply.

The owner requested that the assistant no longer run `npm run validate`:
validation will be run manually by the owner. The manual command and script
remain available; they have not been deleted or weakened. No automatic
validation invocation is added by this implementation.

Historical assistant attempt, before that request: a full-run accepted the existing NuGet audit HTTP
403 exception, then failed with MSB3027/MSB3021 because the running API
(PID 37504) locked the Release executable. Later frontend/dependency gates were
not reached. The owner requested leaving that API running; it has not been
stopped, restarted, rebuilt in place or used to reset owner credentials.
At implementation handoff, the assistant had not built or runtime-tested the
new UI and settings/audit API changes. The owner's subsequent validation report
above supersedes the pending validation status, not the unrecorded runtime
checks. Editor diagnostics reported no errors. The API must be rebuilt/restarted by the owner before
the new settings/audit endpoints are available. Rerun explicit database
initialization to add the `audit_page` index without resetting existing data.

### Owner manual acceptance queue

| Check | Expected | Current result |
| --- | --- | --- |
| Manual validation | Owner runs `npm run validate`, accounting for the running executable lock and approved audit exception | Owner reported expected result on 2026-10-27; output not independently inspected |
| EX-06 acceptance | Owner accepts the publishing interface and settings work package | Accepted by explicit owner instruction on 2026-10-27 |
| Draft authoring | Create category/draft, save metadata, reload and verify persistence | Pending |
| Private preview | Saved sanitized HTML displayed; unsaved editor text is not rendered; anonymous API access denied | Pending |
| Concurrent edit | Two tabs editing one revision yield 412 for the stale tab; its entered values remain until explicit reload | Pending |
| Slug/reference constraints | Duplicate/reserved slug and referenced product/category deletion rejected; safe errors visible | Pending |
| Pricing shell | All six valid configurations save; fractional/out-of-range paisa and contradictory configuration rejected | Pending |
| Lifecycle | Archive/unpublish enforce state/revision; publication remains unavailable until EX-09 | Pending |
| Failure UX | Service outage, expired session and failed save display errors without clearing edited fields | Pending |
| Pagination/accessibility | More than one page of products/categories works; keyboard, narrow viewport and zoom remain usable | Pending |
| Audit persistence | Mutations retain durable intents; projection is idempotent and survives retry | Pending |
| Settings | Unconfigured flag is off; save/reload persists; stale revision returns 412; extra fields/unknown keys rejected; anonymous/CSRF-invalid writes denied | Pending |
| Audit browser | Product/category/settings and projected offline-owner events display; invalid dates/cursors rejected; subsequent pages do not repeat events; no raw configuration exposed | Pending |

EX-06 owner acceptance is recorded without retroactively marking EX-04/EX-05
Done or unrecorded scenarios passed. Reconcile those prerequisite acceptance
gates before accepting dependent public-catalog work. This package acceptance
is not a whole-phase or production-approval claim.

## Previously recorded authentication checks

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

EX-06 is owner-accepted as recorded above. Do not mark EX-04/EX-05 Done or
declare all Phase 2 gates complete without the remaining acceptance evidence.
