# MVP Execution Plan

**Status:** EX-06 is owner-accepted on 2026-10-27 following owner-reported validation with the expected result. Phase 2 prerequisite acceptance evidence and unrecorded manual checks remain outstanding; see [Phase 1 progress](./phase-1-progress.md) and [Phase 2 progress](./phase-2-progress.md).
**Scope:** Downloadable products with public previews, flexible NPR pricing, manually verified QR payments, and secure email fulfillment.  
**Authority:** This plan executes the existing design; it does not replace or silently change it.

## 1. Read first

| Reference | Use during execution |
|---|---|
| [TDD overview](./technical-design-document.md) | Confirmed scope, assumptions, and documentation index |
| [Requirements](./requirements.md) | Requirement IDs, acceptance criteria, and traceability |
| [PRD](./prd.md) | User journeys, features, and UI behavior |
| [Rules](./rules.md) | Authoritative pricing, payment, access, security, and operational behavior |
| [Designs](./designs.md) | Data models, API contracts, frontend routes, state machines, and fulfillment design |
| [Architecture](./architecture.md) | Stack, application boundaries, deployment requirements, and architecture decisions |
| [Phases](./phases.md) | Parent roadmap P1-P9 |
| [Security and QA](./security-and-qa.md) | QA-01 through QA-23 and production launch checklist |
| [Risks and decisions](./risks-and-decisions.md) | OD-01 through OD-11 and launch blockers |
| [Costs and providers](./costs-and-providers.md) | Provider research, quotas, and adoption checks |

No courses, streaming, customer accounts, carts, automated payment gateways, international currencies, or multi-vendor features enter this backlog. Standalone support donations remain disabled by default. No required automated unit/integration tests are added to initial scope; build/lint, manual QA, and security review remain required.

## 2. Execution rules and working structure

1. Work on one small work package at a time. Start only after its explicit prerequisites are complete.
2. Read its linked requirements/contracts before editing; implement backend enforcement as well as UI.
3. Use the approved structure below. Phase 1 has created these directories; feature-level deliverables remain subject to their package gates.
4. Keep development runnable without buying hosting or enabling real payments. Local adapters must be visibly development-only, fail outside Development, and never masquerade as provider verification.
5. Never mark a payment confirmed from a local fake outside isolated manual QA. Do not send production email or use real receiving-account credentials in fixtures.
6. Mark a package **Done** only after its deliverable persists, build/lint applicable to the change pass, and its manual checks have recorded expected/actual outcomes. Unrun checks remain pending, not presumed passed.
7. A provider-dependent check may block integration/release without blocking unrelated local work. A package requiring that check is not fully Done until it passes.
8. Any critical payment/access failure blocks dependent work from being accepted; fix it before proceeding. Production release additionally blocks on unresolved high-severity findings.
9. Keep requirements, rules, designs, and traceability updated when an approved implementation change affects them.
10. Do not commit secrets or overwrite unrelated work. Initialize Git only during implementation with owner authorization; this documentation workspace was not a Git repository when inspected.

### Implementation layout

| Path | Responsibility |
|---|---|
| `frontend` | Next.js App Router, TypeScript, public storefront and admin UI |
| `backend` | ASP.NET Core 10 Web API, feature services, Mongo access, worker and integrations |
| `infra/local` | Local Mongo/object-storage/mail-capture startup configuration and environment template |
| `scripts` | Explicit index/schema maintenance, owner provisioning, backup/restore helpers |
| `docs` | Existing design documents, operational instructions and execution evidence |

Use one API application initially with feature folders, not mandatory extra projects or microservices. Start with documented local startup commands; select/validate container tooling before depending on it. Do not assume Docker, Node, .NET, or Mongo is already installed.

### Roles

- **Implementer:** completes packages, updates contracts, records manual evidence and fixes failures.
- **Owner:** approves material defaults/policies, controls accounts/credentials, verifies receiving-account records, and authorizes launch.
- **Reviewer:** reviews payment/access/auth changes and performs independent prelaunch security review. One person can implement and operate, but independent review cannot be claimed from self-review.

## 3. Work packages

The package tables describe the baseline backlog; current execution status and evidence are recorded in [Phase 1 progress](./phase-1-progress.md) and [Phase 2 progress](./phase-2-progress.md). EX-06 is owner-accepted; EX-04/EX-05 have local implementations with outstanding acceptance evidence. EX-07 through EX-30 have not started. At the owner's request, validation is run manually by the owner, not automatically by the assistant; unrun gates remain pending. IDs EX-01 through EX-30 are execution identifiers; P1-P9 and FR/NFR/F/QA identifiers retain their meanings in the design. A listed dependency is a prerequisite, not just a preferred ordering.

### P1 - Foundation

| ID / package | Depends on | Concrete implementation and deliverable | Completion evidence |
|---|---|---|---|
| EX-01 Establish local foundation | None | Inspect workspace and toolchain; confirm supported Node/Next.js and .NET 10; scaffold web/API using ecosystem tools; set strict types, dependency locks, environment template and safe ignore rules. Document reproducible startup and approved folder layout | Fresh local startup works; web/API builds and web lint pass; no secret values or application features generated beyond foundation |
| EX-02 Establish persistence and runtime configuration | EX-01 | Configure isolated local Mongo and storage/mail-capture development services; implement validated environment configuration, versioned secret/key loading, shared API errors, trace IDs and minimal health endpoints. Add explicit idempotent schema/index setup entry point | Restart preserves local data/keys; missing required configuration fails explicitly; health response has no secrets; QA-22/23 baseline exercised |
| EX-03 Establish delivery pipeline and operational baseline | EX-02 | Add repeatable build/lint and secret/dependency review steps; immutable build artifact convention, structured redacted logs, environment separation and runtime documentation. CI platform remains unbound until selected; do not imply a local command is a deployed pipeline | Build/lint commands reproducible; deliberate secret/log fixture redacted; health/log checks documented; CI activation recorded as a later EX-27 integration obligation |

Coverage: FR-020; NFR-008, NFR-010, NFR-011, NFR-012; F-14. **Gate:** reproducible local foundation and no committed secrets. Deployment provider selection is not required.

### P2 - Administrator and publishing

| ID / package | Depends on | Concrete implementation and deliverable | Completion evidence |
|---|---|---|---|
| EX-04 Protect administration | EX-03 | Provision one owner offline; implement password hashing, MFA/recovery, opaque server sessions, CSRF/origin checks, login throttling and admin authorization. Provide login/logout and documented offline recovery | Anonymous/visitor access denied; MFA required; expired/revoked sessions fail; recovery and CSRF cases from QA-17 recorded |
| EX-05 Implement product and metadata lifecycle | EX-04 | Product/category models, DTOs, CRUD, slug reservations/redirects, tags, sanitized descriptions, license version, revision checks, audit intent/projection and lifecycle endpoints | QA-01: duplicate slug/stale edit/referenced deletion rejected; drafts private. Until EX-09 provides validated assets, publish remains blocked rather than bypassing asset checks |
| EX-06 Build publishing interface and settings | EX-05 | Admin product/category editor, private preview, pricing form shell, lifecycle controls, settings/audit views and field errors. Store only typed non-secret settings; keep payments disabled by default | Owner can save/preview draft; unsafe HTML encoded/sanitized; no secrets in settings response. Final pricing validation arrives in EX-10 and complete publication in EX-09 |

Coverage: FR-001, FR-002, FR-015; NFR-001; F-01/F-11. **Gate:** owner-only authoring works; incomplete product/assets/pricing cannot be published.

### P3 - Public discovery and accessible preview experience

| ID / package | Depends on | Concrete implementation and deliverable | Completion evidence |
|---|---|---|---|
| EX-07 Implement public catalog contracts | EX-06 | Published-only listing/detail/category/search DTOs, indexed bounded search, filters/cursors, cache strategy/invalidation and public metadata/sitemap generation | QA-02/20 using controlled staging fixtures: drafts/archived content excluded, no storage keys, no-login public access, authoritative checkout not based on cache |
| EX-08 Build public pages and policy surfaces | EX-07 | Home/catalog/category/search/detail layouts, public descriptions/previews, responsive/keyboard states and support/privacy/terms/refund page structure. Policy text stays draft until owner approval | QA-19/20: 320px layout, 200% zoom, focus/labels/loading/empty/error states; public metadata checked; no claim that draft policies are legally approved |

Coverage: FR-003, FR-016, FR-019; NFR-004, NFR-005, NFR-006; F-02/F-13. **Gate:** public browsing has no account trap or private data leakage. QA can use isolated valid fixtures before normal asset publication exists.

### P4 - Private assets and safe uploads

| ID / package | Depends on | Concrete implementation and deliverable | Completion evidence |
|---|---|---|---|
| EX-09 Implement asset ingestion and immutable publication | EX-06 | Admin upload intent/completion/validation/revocation, private quarantine, actual size/type/checksum checks, documented trusted scan/manual review, sanitized covers, immutable asset IDs and referenced-asset retention. Wire attachment editor and publication prerequisites | QA-03 and storage portion of QA-11 pass with valid/oversized/disguised files; 100,000,000-byte cap enforced; anonymous private GET denied; unsafe/unscanned assets cannot publish |

Coverage: FR-004; NFR-003; F-03. **Gate:** only validated private assets become deliverable. Real storage-provider access/limits are rechecked in EX-27; local storage is not evidence of R2/S3 adoption.

### P5 - Pricing and request creation

| ID / package | Depends on | Concrete implementation and deliverable | Completion evidence |
|---|---|---|---|
| EX-10 Enforce all pricing modes | EX-08, EX-09 | Backend money-safe validation and policy-version checks; finish admin pricing editor; implement public amount selection for Free, Fixed, PWYW, Suggested, OptionalDonation and FixedPlusDonation | QA-05 covers each mode, floor/ceiling, zero exceptions, tampered price/currency, excess precision and required/top-up split; contradictory configuration rejected |
| EX-11 Persist authoritative order aggregates | EX-10 | Order snapshots, email encryption/keyed lookup, scope-secret hashes, idempotency payload/replay storage, unique request key/indexes, zero eligibility and durable outbox intent. Define required fields for all states | QA-04 acceptance portion and QA-10: same-key retry one order, changed payload 409, replay expiry creates no replacement; zero has no financial receipt; no public fulfill-now shortcut |
| EX-12 Implement contribution separation and checkout/status UI | EX-11 | Product-linked contribution and disabled-by-default standalone donation API, one-product checkout, exact amount display, secure order status capability handling, no-store responses and explicit request/submitted labels | QA-05/10/15: separate free and donation orders; skipped/rejected donation cannot affect free eligibility; support donations never contain product entitlement; secret absent from URLs/logs |

Coverage: FR-005, FR-007, FR-012, FR-013 contracts; F-04/F-05/F-06/F-09. **Gate:** requests and money are correct, but fulfillment and real purchases remain unreleased until later gates.

### P6 - QR submission and manual verification

| ID / package | Depends on | Concrete implementation and deliverable | Completion evidence |
|---|---|---|---|
| EX-13 Implement QR account snapshots and submissions | EX-12 | Versioned QR/account settings, payment instructions with text alternative, reference submission/correction, account-specific normalization, unique reference binding and unsubmitted-request expiry | QA-06/08: disabled account blocks positive requests only; old orders retain account snapshot; Submitted does not expire automatically or grant access |
| EX-14 Implement atomic decisions and review queue | EX-13 | Admin queue/detail, exact account/reference/amount observation, approve/reject with revision and idempotency, immutable receipt, atomic entitlement/audit/outbox update and audited unconfirmed reference release | QA-07/08/09: two concurrent sessions and repeated clicks yield one receipt/initial job; no public confirmation; under/overpayment denied; confirmed reference cannot be released |
| EX-15 Complete payment failure and support flows | EX-14 | Customer status/rejection reason codes, rejection notification jobs, late/rejected-payment support path, linked replacement handling without hidden state resets; audit visibility and review-runbook draft | Manual QA-08/09/10 using known fixtures; real-account verification procedure is explicit, screenshot/reference alone never treated as proof |

Coverage: FR-008, FR-009, FR-015; NFR-002; F-06/F-07/F-11. **Gate:** no unverified paid entitlement; one authoritative receipt per verified order. Real positive checkout remains disabled until EX-26/27/30.

### P7 - Reliable email and secure downloads

| ID / package | Depends on | Concrete implementation and deliverable | Completion evidence |
|---|---|---|---|
| EX-16 Implement durable worker and mail boundary | EX-15 | Embedded job leasing, persistent token/payload preparation, stable provider keys, retry/Retry-After limits, lease recovery and unknown-outcome handling. Implement mail capture plus selected-provider adapter without exposing credentials | QA-12: restart/timeout/duplicate execution preserves funds and intent; retry payload stable; uncertain sends flagged for reconciliation, not blindly resent |
| EX-17 Implement email templates and event reconciliation | EX-16 | Access-only, receipt/access, donation receipt and rejection templates; accepted/delivered/bounced distinction; raw-body webhook signature/freshness/deduplication and out-of-order handling; disable secure-email tracking | QA-13: forged/replayed events cannot change state improperly or approve payment; bounce/quota failure visible. Provider-specific signature checks use the selected provider's current official contract |
| EX-18 Implement access exchange and download URLs | EX-17 | Fragment-token landing with no third-party scripts; explicit exchange; expiring scoped cookie; entitlement/generation/asset/expiry checks; 60-second signed object GET and safe attachment name | QA-04/11: free email flow works; paid access denied before confirmation; cross-order assets blocked; expired/revoked grants fail; scanner GET does not consume access |
| EX-19 Implement retry, correction and reissue interface | EX-18 | Admin failed-delivery view, safe retries, verified-ownership email correction/reissue, generation invalidation, access expiry/support UI and receipt-only donation reissue | QA-12/13/14/15: old grants/sessions fail after reissue; email outage never changes verified funds; no new payment/order created for resend |

Coverage: FR-006, FR-010, FR-011, FR-013 fulfillment; NFR-003, NFR-007, NFR-009; F-05/F-08/F-09. **Gate:** end-to-end local zero and positive fulfillment survives failures/restarts. Live deliverability/worker cadence still needs EX-27/29.

### P8 - Operational administration

| ID / package | Depends on | Concrete implementation and deliverable | Completion evidence |
|---|---|---|---|
| EX-20 Build financial and operational reports | EX-19 | Dashboard, searchable order/verification/delivery queues, contribution totals, receipt-derived reports, Kathmandu reporting boundaries, safe admin CSV and minimal aggregate funnel counters | QA-16: confirmed funds only, required/contribution/refund amounts separate, zero not counted as revenue, CSV injection neutralized |
| EX-21 Implement manual refunds and dispute holds | EX-20 | Audited externally completed refund adjustments, duplicate/over-refund prevention, partial/full refund policy, dispute hold/resolution and entitlement/session revocation checks | QA-18: receipt immutable; full refund/hold blocks new URL issuance; existing URL lifetime explicitly acknowledged; no automated transfer implied |
| EX-22 Implement privacy and quota operations | EX-21 | Retention/deletion and restore-deletion ledger, redacted logs/events, webhook/job cleanup, quota/oldest-job metrics, maintenance/checkout-disable controls and daily reconciliation instructions | QA-21: secrets/PII absent from telemetry; bounded histories/counters; due deletion handled; quota alerts do not falsely claim a hard spend cap |

Coverage: FR-014, FR-017, FR-018; NFR-009, NFR-011; F-10/F-12/F-14. **Gate:** owner can reconcile cash, delivery, support and operational risk without editing database state manually.

### P9 - Recovery, adoption and launch

| ID / package | Depends on | Concrete implementation and deliverable | Completion evidence |
|---|---|---|---|
| EX-23 Implement backup and restore tooling/runbook | EX-22 | Quiesced DB export/index/schema capture, encrypted asset/key backup manifest, independent recovery copy, documented retention and deletion replay; isolate restore and suppress outbound sends | QA-23 local drill restores known free/paid/rejected/refunded orders and file hashes; writers actually stopped for consistent export; lost-key recovery demonstrated |
| EX-24 Implement alerts and rollback procedure | EX-23 | Health/error/job/quota/backup-age alerts, owner notification/acknowledgement path, immutable artifact rollback, config/key recovery and post-restore payment/email reconciliation | QA-22/23 rehearsal identifies incident owner; alerts actionable; rollback does not recreate receipts or resend unreconciled jobs |
| EX-25 Prepare release candidate | EX-24 | Run all QA-01..23 locally/in isolated staging with fault injection, desktop/mobile checks, build/lint and dependency/secret review; collect redacted evidence; fix regressions | Critical/high failures resolved; external-service checks explicitly pending until EX-27; local results never labeled production approval |
| EX-26 Resolve owner launch decisions | EX-01; owner action | Close or explicitly disposition OD-01..11: host/budget, Nepal eligibility, QR account/limits, sending domain, policies/retention/support, scanner, recovery targets and independent reviewer. May run alongside development | Each decision has owner, chosen option, evidence and effect on contracts; blocking unknowns resolved before integration/release |
| EX-27 Integrate the selected deployment/services | EX-25, EX-26 | Activate CI/deployment destination, configure same-origin ingress/TLS/secrets/network restrictions, private storage, real approved email domain/webhooks, worker/scheduler, backups and alert delivery in isolated staging. Validate provider quotas and actual receiving-account identifiers | Provider adoption checklist complete; Nepal/account eligibility verified by owner; worker runs after idle/restart; private GET denied; verified-domain trial and backup schedule succeed |
| EX-28 Complete independent security review | EX-27 | Reviewer assesses actual code/config/dependencies and auth/payment/access/upload boundaries; implement surgical fixes and rerun affected QA | Written findings/disposition, no unresolved critical/high release blockers; this is a performed review, unlike the design threat table |
| EX-29 Rehearse production readiness | EX-28 | Repeat critical/manual integration QA on selected services; controlled Nepal/mobile delivery/latency checks; quota/outage alert drills, independent restore and rollback; owner rehearses account reconciliation | All QA cases have actual expected/observed result, RPO/RTO measured, real-service failures/recovery covered; readiness checklist signed off |
| EX-30 Authorize and perform controlled launch | EX-29; owner approval | Owner approves production cutover; controlled smoke fulfillment, support/contact/policy checks and monitoring; enable positive checkout only after actual account reconciliation and secure fulfillment verified | Release record includes artifacts/config versions, decision approvals, QA/security evidence, recovery result and rollback trigger; disable unsafe checkout immediately if invariant fails |

Coverage: FR-020 and final evidence for all MVP FR/NFR; F-14. **Gate:** technical completion is not public launch. Real payments and unrestricted recipient email require explicit owner approval and resolved adoption dependencies.

## 4. Order of execution and parallel work

**First implementation action:** EX-01. Do not begin by deploying, opening paid subscriptions, integrating a gateway, or implementing a customer-account system.

**Dependency spine:**

`EX-01 -> EX-02 -> EX-03 -> EX-04 -> EX-05 -> EX-06`

After EX-06, EX-07 -> EX-08 can run independently of EX-09. EX-10 waits for both branches. EX-10 through EX-25 follow their listed chain.

EX-26 is independent of EX-02..25 once EX-01 is complete and the owner is ready to make launch decisions. It may be deferred while local development progresses; EX-27 waits for both EX-25 and EX-26. Then EX-27 -> EX-28 -> EX-29 -> EX-30.

Do not parallelize mutations of the same order/payment/grant code without coordination. UI and API contract work may overlap only after DTOs/error/authorization behavior are agreed. Product policy drafting, account eligibility research and scanner evaluation can proceed independently, but changing an assumption must update every affected surface.

## 5. Milestones and stop conditions

| Milestone | Required packages | Demonstration | Not yet authorized |
|---|---|---|---|
| M1 Local foundation | EX-01..03 | Reproducible web/API/data startup, build/lint, health and redaction | Real deployments/accounts or commerce |
| M2 Publish and discover | EX-04..09 | Owner publishes a clean private-file product and visitor finds public preview | Production policies/payment acceptance |
| M3 Correct commerce state | EX-10..15 | All pricing modes and race-safe manual decision with fixture funds | Claiming email delivery or live payment readiness |
| M4 Secure fulfillment | EX-16..19 | Free and verified paid access email, expiry/retry/reissue failures demonstrated | Production service reliability |
| M5 Operable local release candidate | EX-20..25 | Reports/refunds/privacy plus backup/restore/alerts and local regression evidence | Public launch |
| M6 Adopted and reviewed staging | EX-26..28 | Real approved services integrated, independent review remediated | Unreviewed production traffic |
| M7 Approved launch | EX-29..30 | Selected-service recovery/QA and owner-approved controlled release | Future-scope features without new design |

Stop and record a blocker if an order can receive paid access without verification, a duplicate confirmation creates a second financial fact, private assets become public, donation blocks free fulfillment, secrets appear in outputs, or restore cannot recover usable keys/assets/orders. Do not hide these failures behind retries, manual database fixes or a "pilot" label.

## 6. Acceptance evidence and progress updates

For each package, record:

| Field | Required content |
|---|---|
| Work package | EX ID and parent phase |
| Status | Pending / In progress / Blocked / Done |
| Implemented deliverable | Actual changed paths and persisted behavior; not proposed paths alone |
| Contract coverage | Relevant FR/NFR/F/BR IDs and API/model changes |
| Validation | Exact build/lint/manual steps, environment, expected versus observed result, QA IDs |
| Decision/blocker | OD ID, owner, required resolution; no fake pass for unrun provider checks |
| Review | Reviewer/disposition for security-sensitive changes |
| Handoff | Remaining work and next dependency-ready package |

The package tables are a baseline backlog, not fabricated execution results. During implementation, create a compact progress ledger alongside this plan or use the chosen issue tracker and link it here. Record a redacted release evidence file when executing EX-25/29/30; never store passwords, capabilities, signed URLs, private payment evidence or customer emails as QA artifacts.

### Universal completion check

- Deliverable and relevant documentation persist in the workspace.
- New state transitions and errors agree with the design and are wired across API, storage and UI.
- Applicable build/lint and required manual scenarios pass.
- No unrelated changes, new hidden scope, or success-shaped fallback masks an error.
- Feature release flags and unavailable dependencies fail safely.
- Phase and launch prerequisites are not bypassed.

## 7. Deferred execution

After launch, prioritize focused automated regression coverage for safe money parsing, duplicate/racing approvals, entitlement/asset authorization, grant expiry/revocation, and outbox restart/unknown-send handling. Automation is a later hardening backlog, not a newly imposed initial prerequisite.

Courses, standalone articles, streaming, customer libraries/accounts, subscriptions, automated gateways, international currency and marketplace features need separate approved requirements and design work before scheduling implementation.
