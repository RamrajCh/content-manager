---
type: Risk Register
title: Risks, Dependencies, and Open Decisions
description: Risk register, dependencies, and unresolved launch decisions.
tags: [risks, decisions, operations]
---

# Risks, Dependencies, and Open Decisions

> Part of the [Technical Design Document](./technical-design-document.md). Stable requirement, feature, rule, phase, QA, and source identifiers are shared across this documentation set.

Risk register and unresolved launch decisions. Confirmed constraints and assumptions remain in the [overview](./technical-design-document.md#confirmed-constraints-assumptions-and-recommendations).

## 7. Risks, dependencies, and open decisions

### 7.1 Risk register

| Risk | Impact | Mitigation / owner |
|---|---|---|
| Manual approval error or fabricated evidence | Incorrect paid access or lost funds | Actual account reconciliation, exact comparison, immutable receipts, reasoned decisions; owner |
| Reference preclaim/reuse | Legitimate payment blocked | Unique claim index, no automatic approval, audited unconfirmed release/support; owner |
| Free host sleep/quota/suspension | Delayed email/status, SEO problems | Durable jobs, explicit pilot limitations, choose suitable runtime before production |
| Email outage, typo, spam filtering | Customer cannot retrieve file | Quota reserve, verified domain, signed bounce events, independent resend/support |
| Link forwarding or leaked capability | Unauthorized redistribution | Short grants/URLs, no-referrer, revocation, CSP; bearer-link residual risk accepted |
| Malicious upload/archive or private bucket misconfiguration | Malware/exposure | Admin-only quarantine, trusted scan, attachment delivery, anonymous-GET QA |
| Lost keys/DB/assets or account compromise | Irrecoverable purchases, records or secrets | Independent encrypted backup, offline recovery, restore drill, provider MFA |
| MongoDB official free-backup documentation conflict | False recovery assumptions | Independent export/restore required; obtain dashboard/support confirmation |
| Deferred deployment and Nepal service eligibility | Cannot safely launch | Do not treat a nearby region as eligibility; confirm checklist before adoption |
| No automated initial regression tests | Repeated money/access bugs | Mandatory manual race/failure matrix, build/lint/review; later focused automation |
| Undefined legal/tax/support retention policy | Compliance/support risk | Qualified Nepal advice, published terms, conservative explicit defaults pending confirmation |
| Sole administrator unavailable | Verification backlog and recovery failure | Publish real review expectations, offline recovery/runbook, disable new payments if unable to fulfill |

### 7.2 Open decisions and launch dependencies

| ID | Decision / current treatment | Owner / required gate |
|---|---|---|
| OD-01 | Hosting/topology/region/budget deferred; no selected deployment | Owner before production; satisfy runtime, origin, worker and backup gates |
| OD-02 | Receiving QR provider/account, merchant eligibility, limits/fees, reusable reliable transaction IDs | Owner + bank/wallet before enabling positive checkout |
| OD-03 | Provider onboarding/billing from Nepal and acceptable local/international payment instrument | Owner + each provider; do not assume a Nepal card is accepted |
| OD-04 | Owned sending domain/DNS and email account approval | Owner before real-recipient delivery |
| OD-05 | Donation terminology, standalone support enablement, tax/refund/consumer obligations | Owner + qualified Nepal adviser; standalone disabled meanwhile |
| OD-06 | Final formats/allowlist, scan tooling and storage budget | Owner before catalog publication; default cap confirmed, allowlist proposed |
| OD-07 | Monetary limits/precision, mismatch/manual reconciliation policy | Owner + receiving provider before payment launch |
| OD-08 | Link/support duration, email correction identity checks, retention and deletion | Owner before publishing terms; proposed defaults not legal advice |
| OD-09 | Availability, email latency, RPO/RTO and access-support commitment | Owner approves targets against chosen deployment |
| OD-10 | Atlas current backup/region/network limits and pilot production suitability | Owner/provider dashboard/support before adoption |
| OD-11 | Independent security reviewer and documented acceptance of deferred automation risk | Owner before launch |

No unresolved provider/account fact is a blocker to finishing this document; it may be a blocker to deploying the application or accepting real money.
