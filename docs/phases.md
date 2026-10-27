---
type: Development Roadmap
title: Development Phases
description: Implementation dependencies, phase deliverables, manual acceptance gates, and deferred scope.
tags: [implementation, planning, roadmap]
---

# Development Phases

> Part of the [Technical Design Document](./technical-design-document.md). Stable requirement, feature, rule, phase, QA, and source identifiers are shared across this documentation set.

Dependency-aware implementation roadmap and manual acceptance gates. Phase 1 local implementation has started; [Phase 1 progress](./phase-1-progress.md) records actual evidence and outstanding gates.

For the actionable work-package backlog, dependencies, deliverables and execution evidence, follow the [MVP execution plan](./execution-plan.md).

## 3. Development phases

All P1-P9 are MVP implementation phases; no durations are estimated. The TDD does not execute them. Global phase definition of done: requirements and related API/model/rules agree, build/lint succeed where relevant, manual evidence is saved without secrets, and no critical access/payment defect remains. Technical completion is not a launch approval.

| Phase | Objective, tasks, deliverables | Dependencies | Coverage | Acceptance gate / manual QA | Risks / phase-specific done |
|---|---|---|---|---|---|
| P1 | Foundation: project boundaries, strict types, API error conventions, config/secrets, Mongo connection/index scripts, logging/health, deployable local containers and CI builds | None | FR-020; NFR-008, NFR-010, NFR-011, NFR-012 | QA-22/23 baseline; secrets absent; local restart preserves DB state | Hosting deferred; production pipeline destination remains open; document environments without purchasing services |
| P2 | Admin auth/MFA, product/category/tag CRUD, draft preview, publication rules, basic settings and audit | P1 | FR-001, FR-002, FR-015; NFR-001 | QA-01/17; anonymous admin requests denied; stale edit conflicts visible | Recovery codes/credential storage; done when only owner can publish |
| P3 | Public catalog, search/filter/detail, accessible responsive UI, public policies, metadata/sitemap and draft exclusion | P2 | FR-003, FR-016, FR-019; NFR-004, NFR-005, NFR-006 | QA-02/19/20; no draft/private metadata leakage | Cached unpublish; done when public pages are safe without accounts |
| P4 | Quarantined asset uploads, validation/manual scan process, private storage, immutable asset references, public covers | P2 | FR-004; NFR-003 | QA-03/11 storage boundary portion; oversize/unsafe files blocked | Storage credentials/quota; done when only validated private assets attach to products |
| P5 | Pricing matrix, money validation, order snapshots, free eligibility, donation separation, idempotent create/status contracts | P3, P4 | FR-005, FR-007, FR-012, FR-013 contract | QA-05/06/10/15 policy cases; free eligibility recorded but fulfillment UI not released until P7 | Caps/provider precision unresolved; no real purchases enabled |
| P6 | QR settings, reference submission, admin approval/rejection, duplicate-reference index, embedded receipt/audit/outbox and request expiry | P5 | FR-008, FR-009, FR-015; NFR-002 | QA-06/07/08/09; race yields one receipt, unverified order gets no entitlement | Receiving-account reconciliation; done when all decisions are server-authoritative and auditable |
| P7 | Email templates/provider/webhooks, worker lease/retries, secure access exchange, signed downloads, resend/email correction | P6 | FR-006, FR-010, FR-011, FR-013 fulfillment; NFR-003, NFR-007, NFR-009 | QA-04/11/12/13/14/15; email outage never changes payment | Verified domain/host execution; done when zero and positive flows fulfill safely after restart |
| P8 | Dashboard/filters, financial/donation CSV, minimal funnel counters, support/refunds/disputes, quota views | P7 | FR-014, FR-017, FR-018 | QA-16/18/21; totals reconcile, refund suppresses new URL issuance | Reporting must not sum pending orders; done when owner can operate/reconcile daily |
| P9 | Provider adoption checks, security review, full manual regression, monitoring alerts, encrypted backups, timed restore, rollback, production smoke/release evidence | P8 | FR-020; all NFR final evidence | All QA cases and [security and QA launch gates](./security-and-qa.md#93-production-launch-gate); no critical/high unresolved payment/access risk | Deployment/account/legal/budget decisions; no real-money launch without readiness evidence |

**Critical path:** P1 -> P2 -> P3/P4 -> P5 -> P6 -> P7 -> P8 -> P9. P3 and P4 can proceed independently after P2. Policy drafting and official-provider evaluation can run alongside P2-P4. Security begins in P1, not just P9.

**Post-launch:** add critical-path automated tests (money, approval races, access denial, outbox retry), improved dashboards, verified localization demand, operational tuning. **Future only (FR-021/F-15):** courses, standalone articles, streaming/transcoding, accounts/libraries, carts, advanced versioning, automated gateways, multiple currencies, subscriptions. Multi-vendor functionality is not an implied roadmap commitment.
