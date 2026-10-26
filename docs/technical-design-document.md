# Technical Design Document - Digital Content and Optional Donation Platform

**Status:** Design baseline for implementation; not a production-readiness certification.  
**Research reviewed:** 2026-10-26. Provider terms must be rechecked before adoption.  
**Owner:** Single platform owner, also acting as administrator and publisher.  
**Implementation:** Phase 1 local foundation exists in separate frontend/backend applications; no production deployment or commerce functionality. See [Phase 1 progress](./phase-1-progress.md) for evidence and outstanding gates.

## Documentation map

This file is the overview and entry point, not a duplicate monolithic TDD. Read the linked documents as one design baseline; each section has one authoritative home. Original section numbers are retained so existing references remain recognizable.

| Document | Contents |
|---|---|
| [Requirements](./requirements.md) | Actors, functional/non-functional requirements, acceptance criteria, traceability |
| [PRD](./prd.md) | Product definition, user journeys, features, UI requirements |
| [Rules](./rules.md) | Pricing, payment, access, security, privacy, operational and engineering rules |
| [Designs](./designs.md) | Frontend, data models, REST contracts, state diagrams, storage and email workflows |
| [Phases](./phases.md) | Implementation dependencies, deliverables, manual gates and future scope |
| [Execution plan](./execution-plan.md) | Concrete implementation packages, dependencies, acceptance evidence, milestones and launch stop conditions |
| [Architecture](./architecture.md) | Technical stack, decisions, system/component/deployment diagrams, backend structure and rollback |
| [Risks and decisions](./risks-and-decisions.md) | Risk register, dependencies and unresolved launch decisions |
| [Costs and providers](./costs-and-providers.md) | Free-tier research, Nepal availability, capacity/cost model and adoption checklist |
| [Security and QA](./security-and-qa.md) | Threat controls, manual QA cases and production-readiness gate |
| [References](./references.md) | Official research sources and evidence qualifications |

## Executive summary

Build a Nepal-first digital-download storefront using Next.js with TypeScript and an ASP.NET Core Web API. Visitors browse product descriptions and previews without accounts. Every downloadable product supports an explicitly configured pricing policy. Free and permitted zero-price requests receive secure links by email without waiting for a donation. Required payments use a configurable QR and manual verification against the receiving account, never automatic approval from a screenshot.

Use a modular monolith, MongoDB document persistence, private S3-compatible object storage, and a durable email queue embedded with the order. Keep payment verification, access entitlement, and delivery status separate. The backend owns prices, state transitions, and authorization. Private files never become permanent public downloads.

The original brief included courses, articles, and online media. The owner explicitly narrowed the MVP to **downloadable products with public descriptions/previews only**, with **no courses right now**. This document reflects that newer decision. Downloadable audio/video files do not imply streaming or a course player.

Recommend MongoDB as the logical database and evaluate Atlas Free as a small pilot service, not an unlimited production database. Recommend R2-compatible storage, Resend-compatible transactional email, and Cloudflare Web Analytics plus first-party aggregate business counters. All service selections are recommendations, not approved subscriptions. Deployment decisions are deferred at the owner's direction; document production requirements and candidate limitations without silently selecting a paid deployment.

### Confirmed constraints, assumptions, and recommendations

| ID | Classification | Decision or assumption | Consequence |
|---|---|---|---|
| C-01 | Confirmed | Single owner; publisher is an admin capability | One admin account; no vendors, commissions, or payouts |
| C-02 | Confirmed | Next.js + TypeScript; ASP.NET Core Web API; NPR | REST business boundary; no client-authoritative commerce |
| C-03 | Confirmed | Downloads and public product previews only | No standalone articles, courses, streaming, or paid online reader |
| C-04 | Confirmed | No mandatory visitor accounts | Email and scoped bearer capabilities replace customer accounts |
| C-05 | Confirmed | Email secure links for free and zero-price requests immediately | "Immediately" means enqueue on acceptance; provider/host outages may delay sending |
| C-06 | Confirmed | Optional donations do not delay free access | Free request and donation are separate aggregates |
| C-07 | Confirmed | Provider-neutral QR, manual verification | Receiving account/provider must be validated before accepting payments |
| C-08 | Confirmed | Maximum 100 MB per file; small initial catalog | Limit is 100,000,000 bytes; allow multiple files per product subject to A-02 |
| C-09 | Confirmed | Prefer free-only starting services; decide deployments later | No claim of guaranteed free or production-suitable hosting |
| C-10 | Confirmed | No required automated unit/integration tests initially | Manual release evidence and security review are mandatory |
| A-01 | Assumption | One product per order; no cart, coupons, bundles, subscriptions, or shipping | Simpler price snapshot and entitlement |
| A-02 | Assumption | At most 5 downloadable assets per product; initial live catalog budget 4 GB | Additional budget needed for originals, retained versions, and backups |
| A-03 | Assumption | Free downloads require a delivery email, but not registration | Email used only for fulfillment/support, not marketing |
| A-04 | Assumption | New payment requests expire in 48 hours; submitted requests remain reviewable | Expiry does not discard real funds |
| A-05 | Assumption | Email access links expire after 72 hours; exchange sessions last 15 minutes; signed object URLs last 60 seconds | Admin can reissue links without new payment |
| A-06 | Assumption | Positive amounts range from NPR 1.00 to NPR 100,000.00; two decimal places, exact matching | Provider limits may require lowering caps before launch |
| A-07 | Assumption | Payment reference is required; public screenshot upload is not MVP | Reduces sensitive uploads and storage/abuse exposure |
| A-08 | Assumption | An unpaid free request is fulfilled independently from any donation; positive PWYW selections wait for verification | Optional contribution is distinct from chosen payable order value |
| A-09 | Assumption | Standalone support donations are specified but disabled by default | Separate from per-product optional contributions; owner/provider/legal confirmation required to enable standalone support |
| A-10 | Assumption | English UI initially; Nepali localization later | NPR formatting and Asia/Kathmandu reporting still supported |
| R-01 | Recommendation | MongoDB order aggregate with atomic compare-and-set and embedded outbox | Avoid dependence on cross-document transactions for payment correctness |
| R-02 | Recommendation | Same-origin API routing; server-side admin sessions and MFA | Avoid browser tokens in local storage and broad CORS |
| R-03 | Recommendation | No hard lifetime download count; abuse rate limits instead | Signed URLs cannot guarantee one-time use or prove a completed download |
| R-04 | Recommendation | Prefer paid production capacity if free tiers cannot satisfy launch gates | Budget decision remains open, not an MVP purchasing requirement |

Assumptions are explicit proposed defaults, not statements of Nepal law, provider entitlement, or proven traffic demand. Changes to A-04 through A-07 require coordinated changes to validation, UI, models, QA, and policies.

## 10. Glossary

| Term | Meaning |
|---|---|
| Product | Downloadable item with public description/preview, pricing policy and private assets |
| Order/request | One product fulfillment or one independent support contribution, with snapshotted terms |
| Submission | Customer's payment claim/reference, not a verified transfer |
| Receipt | Immutable internal verified-funds record, not automatically a statutory tax invoice |
| Required amount | Payment needed for this chosen order, including positive PWYW selection |
| Contribution | Voluntary top-up or separate support amount; legal/tax characterization unresolved |
| Entitlement | Backend-recorded eligibility for particular assets independent of email status |
| Grant/capability | Secret emailed bearer token authorizing scoped access until expiry/revocation |
| Signed URL | Very short storage bearer authorization to one object |
| Outbox | Durable send intent committed with business state, later processed by worker |
| Accepted / Delivered | Email provider accepted request / later delivery event; neither proves person read or downloaded |
| RPO / RTO | Maximum acceptable data-loss window / recovery duration; provisional targets here |
| Pilot | Limited evaluation with explicitly accepted service constraints, not implicit production readiness |

### Final consistency record

- Download-only scope supersedes the broader original content brief throughout requirements, routes, phases and future scope.
- Six pricing modes share one backend validation model; optional donation and chosen positive payable orders have distinct rules.
- NotRequired/Confirmed product orders alone get ordinary eligibility; support donations never get assets.
- Payment approval, receipt, entitlement and durable send intent are one atomic order mutation; worker/email outcomes cannot reverse funds.
- APIs, models, diagrams and QA use the same independent payment, entitlement, capability and email states.
- Traceability covers every functional/non-functional requirement, with future scope explicitly separated.
- Deployment and Nepal provider eligibility remain unresolved by design; all unverified details are labeled, including conflicting Atlas backup documentation.
- This document is complete as a design baseline, not evidence that software, security controls, provider accounts or production recovery have been implemented.
