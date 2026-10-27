---
type: Readiness Checklist
title: Security, Manual QA, and Production Readiness
description: Design threat controls, manual QA cases, and production-launch requirements.
tags: [security, qa, production-readiness]
---

# Security, Manual QA, and Production Readiness

> Part of the [Technical Design Document](./technical-design-document.md). Stable requirement, feature, rule, phase, QA, and source identifiers are shared across this documentation set.

Design threat assessment, manual QA matrix, and launch checklist. This is not a completed code security review or evidence that an application has been deployed.

## 9. Security, manual QA, and production readiness

### 9.1 Threat-to-control assessment

This is design threat analysis, **not** a completed vulnerability assessment of code.

| Threat | Preventive/detective controls | Verification |
|---|---|---|
| Admin compromise/session theft | MFA, secure cookies, server expiry, recovery codes, rate limits, CSP, logs | QA-17 |
| IDOR/broken access control | Per-order scoped capabilities, admin policies, session generation/asset binding | QA-10/11 |
| Forged payment/price tampering | Backend snapshot, actual account reconciliation, exact amounts, no public confirm | QA-05/07 |
| Replay/race/duplicate references | Unique indexes, revision filters, stable idempotency, embedded receipt/outbox | QA-09/12 |
| Malicious files/public exposure | Quarantine/scan/type limits, private buckets, attachment downloads | QA-03/11 |
| XSS/CSRF/injection/open redirect | Sanitization/encoding/CSP, antiforgery/origin, bounded typed queries, fixed return destinations | QA-17 |
| Email enumeration/spam/DoS | Generic responses, per-IP/email/order limits, no public resend, quotas | QA-10/21 |
| Secrets in logs or supply-chain compromise | Redaction, least privilege, secret/dependency review, pinned artifacts | QA-17/21 |
| Data loss/quota exhaustion | Independent backup, restore rehearsal, monitoring, stop-work thresholds | QA-22/23 |

### 9.2 Manual QA matrix

Run in isolated staging with controlled mailboxes and known receiving-account records/test reconciliation fixtures. Never simulate success against real funds without owner authorization. Failure severity indicates release impact, not a claim of an observed vulnerability.

| ID | Preconditions | Steps | Expected result | Severity if failed |
|---|---|---|---|---|
| QA-01 | Admin/MFA; draft and referenced product | Create/edit/preview/publish/unpublish/archive; attempt delete referenced item and stale edit | Valid transitions; private preview; 409 referenced delete, 412 stale revision; audit | High |
| QA-02 | Draft, published, archived products | Browse/search/filter/category; query draft slug; unpublish cached product | Only published public data; no login; cache invalidates <=60s; checkout immediately blocked | High |
| QA-03 | Storage/quarantine configured | Upload valid PDF/ZIP, >100MB file, disguised HTML, unsafe archive, forged completion | Invalid files cannot validate/attach; quarantine private; measured size/hash/scan required | Critical |
| QA-04 | Free and zero-allowed products, working email | Request with email, skip every donation; repeat same create key | NotRequired, Eligible, one job/order; email link; zero receipt count | Critical |
| QA-05 | Every pricing mode | Submit zero/positive/min-1/max+1, excess decimal, negative, wrong currency, modified fixed price, top-up | Matrix enforced; no hidden free donation; exact itemization; invalid config rejected | Critical |
| QA-06 | QR account enabled/disabled; pending order | Create/reload, change QR settings, disable account, create zero order | Snapshot retained; no duplicate; disabled account blocks positive only; QR text alternative | High |
| QA-07 | Positive Pending/Submitted order | Submit screenshot-equivalent claim/reference; attempt public/admin bypass; approve against matching account funds | Submission does not authorize; only admin observation creates one receipt/entitlement/job | Critical |
| QA-08 | Missing/under/over/late funds | Reject with reason; expire unsubmitted request; submit after expiry; review old Submitted | No wrong paid access; late funds support path; submitted remains reviewable; reason/audit preserved | Critical |
| QA-09 | Two admin sessions; duplicate reference | Race approvals and approve/reject; retry command; claim same ref in another order; audited unconfirmed release | One decision/receipt; 409/412/replay; confirmed reference never released; no double financial count | Critical |
| QA-10 | Two orders and unrelated capabilities | Swap IDs/secrets, omit auth, enumerate emails/orders, replay create key with changed payload | No data leakage; generic errors, rate limits; changed key payload conflict | Critical |
| QA-11 | Private file and eligible/unverified orders | Anonymous object GET; exchange wrong/expired token; wrong asset; obtain signed URL and test after expiry; revoke | Private GET denied; bound entitlement; new URL denied on revocation; residual short URL lifetime documented | Critical |
| QA-12 | Eligible queued job; controllable provider failures | Fail send, timeout after acceptance, restart worker mid-lease, retry approval and job | Payment immutable; no lost intent; stable key/payload; unknown outcome reconciled; no duplicate receipt | Critical |
| QA-13 | Low email quota/outage/bounce | Exhaust quota, bounce address, replay signed webhook, send forged/out-of-order events | Visible deferred/failure, no false delivery/financial reversal; signatures/dedupe/state precedence hold | High |
| QA-14 | Expired link and customer email typo | Admin validates ownership, corrects recipient/reissues; try old token/session; scanner GET landing | New generation only; old exchange/session blocked; scanner doesn't consume; no new payment | High |
| QA-15 | Optional donation; support flag off/on in staging | Free request then cancel/reject donation; enabled standalone donation confirm; fixed top-up verify | Free access unaffected; donation receipt no assets; disabled support rejected; components reconcile | Critical |
| QA-16 | Confirmed/pending/rejected/zero/refunded data | Filter queues/reports, export CSV with dangerous title, compare totals by Kathmandu day | Only confirmed cash; refund separate; safe CSV; counters not accounting | High |
| QA-17 | Admin/visitor/expired session, security config | MFA/login/logout/recovery; CSRF request; malicious HTML/filter/redirect; inspect cookies/logs/headers/dependencies | Server auth, anti-CSRF and sanitization; no secret logs; least privilege; documented recovery | Critical |
| QA-18 | Confirmed paid order | Record external partial/full refund twice; over-refund; dispute/release; request URL | No duplicate/over-refund; full refund/hold stops new access; original receipt intact | Critical |
| QA-19 | Mobile/desktop, keyboard/screen reader | Complete free/fixed flow at 320px and 200% zoom; errors/empty/loading; QR alternative/policies | Accessible labeled controls/focus, visible required vs voluntary, no account trap | High |
| QA-20 | Public/private routes, Nepal mobile sample | Inspect metadata/canonical/sitemap/robots; measure public page/API warm and cold behavior | Public-only index, private noindex/no-store, targets measured honestly; no unsupported SLA claim | Medium |
| QA-21 | Logging/events and retention dataset | Inspect analytics payloads, logs, provider tracking; submit spam events; run retention job | No email/reference/token URLs; tracking off for secure mail; bounded counters; overdue data handled/audited | High |
| QA-22 | Active monitoring/backup runtime | Trigger readiness/5xx/job/quota/backup-age thresholds; acknowledge incident | Owner receives alert, dependencies identifiable, stop-work and recovery procedure executable | High |
| QA-23 | Encrypted independent backups and previous image | Quiesce/export; isolate restore with keys/assets/indexes; hash check; replay deletions; rollback; reconcile newer payments/sends | RPO/RTO measured, no stale refund revival/email burst, paid/zero/rejected boundaries preserved | Critical |

### 9.3 Production launch gate

Use this checklist at implementation launch; unchecked items are intentional because no application exists yet.

- [ ] Owner approves scoped requirements, pricing, support/refund/privacy terms, limits and retention.
- [ ] OD-01 through OD-11 resolved or explicitly documented as safe nonblocking deferrals; no blocking unknown hidden in "free tier".
- [ ] Commercial-use eligibility, Nepal onboarding, receiving-account permission, sending domain and actual recipient trial verified.
- [ ] Admin MFA/session recovery, server authorization, antiforgery, upload safety and private storage checked.
- [ ] No unverified paid access; no forced donation for free content; exact amounts and duplicate approval controls pass.
- [ ] Email outage/retry/bounce/reissue/restart passes without duplicated financial facts.
- [ ] Security review completed by qualified reviewer; critical/high findings resolved or launch stopped.
- [ ] All critical/high manual QA cases pass; accessible/responsive/SEO evidence recorded.
- [ ] HTTPS, production config, secret rotation, dependency checks and no sensitive logs verified.
- [ ] Monitoring alerts active; owner can observe quota/email/payment backlog and incident path.
- [ ] Independent DB/assets/keys backup and full restore/rollback drills pass with measured recovery.
- [ ] Worker execution/scheduler and backup runner actually operate on selected host; cold starts and suspension behavior accepted.
- [ ] Manual production smoke checks with controlled fulfillment, support contact and maintenance/checkout-disable switch pass.
- [ ] Residual link-sharing/manual-review/no-automated-regression risk accepted in release record.
