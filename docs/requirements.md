# Requirements and Traceability

> Part of the [Technical Design Document](./technical-design-document.md). Stable requirement, feature, rule, phase, QA, and source identifiers are shared across this documentation set.

Functional and non-functional requirements, acceptance criteria, actors, and traceability. Related implementation details are in [designs](./designs.md), [architecture](./architecture.md), and [PRD](./prd.md).

## 1. Requirements

### 1.1 Actors and boundaries

- **Visitor/customer:** browses previews; requests email fulfillment; chooses eligible amounts; submits a payment reference; checks their own request using a scoped secret.
- **Administrator/publisher:** creates content, manages files/settings, verifies payments against account records, retries delivery, handles support/refunds, and operates the site.
- **External systems:** receiving bank/wallet, email provider, object storage, and hosting/database services. A receiving account is not an automated gateway integration.
- No public signup, customer password, shopping cart, marketplace, or course system is needed for MVP.

### 1.2 Functional requirements and acceptance criteria

Priority is Must/Should/Could. Must requirements are MVP launch requirements. A feature disabled by policy must still reject unauthorized/disabled operations server-side.

| ID | Requirement | Priority / release | Observable acceptance criterion | Rules |
|---|---|---|---|---|
| FR-001 | Admin creates, previews, edits, publishes, unpublishes, archives, and safely deletes products | Must / MVP | Draft is invisible publicly; valid publish makes preview discoverable; referenced products cannot be hard-deleted | BR-01, BR-02 |
| FR-002 | Admin manages unique slugs, categories, tags, covers, and SEO fields | Must / MVP | Duplicate slug is rejected; a referenced category cannot be deleted without reassignment | BR-01 |
| FR-003 | Visitors browse, search, filter, and view public product previews without an account | Must / MVP | Search returns only published products; empty/error/loading states exist | BR-02 |
| FR-004 | Admin uploads and attaches validated private product assets | Must / MVP | A file over 100,000,000 bytes or quarantined asset cannot be published/downloaded | BR-03 |
| FR-005 | Server enforces each product's free, fixed, PWYW, suggested, optional-donation, or fixed-plus-contribution pricing | Must / MVP | Tampered amounts/currency are rejected; pricing matrix in [Rules 6.2](./rules.md#62-pricing-access-and-donation-matrix) holds for each mode | BR-04, BR-05 |
| FR-006 | Valid free and permitted zero-price requests enqueue secure email delivery | Must / MVP | Zero requires no QR submission and creates no financial receipt | BR-06, BR-10 |
| FR-007 | Server creates positive orders with a versioned price and QR/account snapshot | Must / MVP | Refresh/retry does not create another order for the same idempotency key | BR-07 |
| FR-008 | Customer submits a reference and views only their own masked payment status | Must / MVP | Submission remains unverified; guessed order ID or another order's secret cannot disclose status | BR-07, BR-08 |
| FR-009 | Admin approves/rejects against reliable receiving-account evidence | Must / MVP | Only authenticated admin can decide; exact amount/reference checked; concurrent approval records one receipt | BR-08 |
| FR-010 | Eligible customer exchanges emailed access capability for temporary private downloads | Must / MVP | Unverified paid order gets no grant; expired/revoked capability cannot mint a URL | BR-10, BR-11 |
| FR-011 | System tracks delivery jobs, retries, bounces, failures, and admin reissues | Must / MVP | Simulated send failure leaves confirmed payment unchanged; retry creates no extra receipt/order | BR-12 |
| FR-012 | Free-product donation opportunity is separate from access; fixed top-up is itemized | Must / MVP | User skips donation and receives free link; fixed required and voluntary components reconcile separately | BR-04, BR-06 |
| FR-013 | Optional standalone support donation follows QR verification without product entitlement | Should / MVP, disabled by default | Disabled API returns policy error; when enabled verified donation sends receipt, never product access | BR-06, BR-08 |
| FR-014 | Admin sees searchable orders, verification queue, delivery queue, financial summaries, and CSV export | Must / MVP | Reports count only verified receipts as received funds; refunds/top-ups are separately visible | BR-09, BR-14 |
| FR-015 | Admin configures QR/account instructions, limits, and non-secret settings with audit history | Must / MVP | Missing/disabled account blocks positive checkout; existing orders keep their account snapshot | BR-07, BR-15 |
| FR-016 | Public previews expose SEO metadata; private/admin/status routes are not indexed or tracked | Must / MVP | Sitemap excludes drafts/private routes; public share metadata is correct | BR-02, BR-16 |
| FR-017 | System records minimal funnel events and durable commerce counters | Must / MVP | Verified payment counts derive from receipts, not client events; no email/token reaches analytics | BR-14, BR-16 |
| FR-018 | Admin records disputes and manual external refunds without pretending to execute payments | Must / MVP | Refund record includes amount/reference/reason/actor; full refund blocks new access grants/URLs | BR-09 |
| FR-019 | Public privacy, terms, fulfillment/refund, and support information are visible | Must / MVP | Customer sees email use, verification delay, link expiry, and support route before submitting | BR-13 |
| FR-020 | Platform operates with authenticated administration, monitoring, backup, restore, and release gates | Must / MVP | Documented restore and critical manual QA pass before real-money launch | BR-15, BR-17 |
| FR-021 | Courses, articles, streaming, customer accounts/libraries, international payments, and automatic gateways | Could / Future | Absent from MVP routes/contracts; any later addition requires updated scope/security design | BR-18 |

### 1.3 Non-functional requirements

Targets below are provisional engineering objectives, not established business SLAs. Do not claim compliance from design alone.

| ID | Requirement / proposed target | Priority / release | Acceptance evidence | Rules |
|---|---|---|---|---|
| NFR-001 | Every privileged operation enforces authorization and MFA-protected admin sessions | Must / MVP | Unauthenticated and visitor-capability requests receive 401/403; CSRF checks exercised | BR-15 |
| NFR-002 | Price/payment/entitlement integrity survives retries, races, and restarts | Must / MVP | Two simultaneous approvals yield one receipt and one initial delivery generation | BR-08, BR-12 |
| NFR-003 | Private files and secrets are not publicly exposed | Must / MVP | Anonymous object GET fails; URL expiry/revocation and redacted logs verified | BR-03, BR-11 |
| NFR-004 | Responsive WCAG 2.2 AA-oriented flows | Must / MVP | Keyboard-only checkout/admin review; labels, contrast, focus, 320px layout and 200% zoom checked | BR-19 |
| NFR-005 | Public preview performance: provisional LCP <=2.5s, INP <=200ms, CLS <=0.1 at p75 | Should / MVP | Nepal mobile measurements when samples exist; prelaunch lab evidence labeled lab-only | BR-16 |
| NFR-006 | Warm API p95 <1s excluding external services; measure cold-start separately | Should / MVP | Manual representative small-catalog runs record payload, region, and host; no free-tier latency promise | BR-17 |
| NFR-007 | Eligible email job attempt within 5 minutes on an active host; durable pending work after restart | Must / MVP | Outage/restart drill processes overdue jobs; a sleeping host is a documented launch blocker for target | BR-12 |
| NFR-008 | Proposed availability 99.5% monthly; RPO <=24h; RTO <=8h | Should / MVP | External checks, backup-age evidence, timed restore; not claimed for free pilot | BR-17 |
| NFR-009 | Minimize data; documented retention/deletion and no marketing enrollment | Must / MVP | Retention job/manual run removes due data, preserves legally required records only after advice | BR-13 |
| NFR-010 | HTTPS, secrets rotation, least privilege, vulnerability review | Must / MVP | Secret scan, dependency review, cookie/header checks, credential rotation rehearsal | BR-15 |
| NFR-011 | Cost and quota visibility; no assumed hard cap from alerts | Must / MVP | Owner verifies usage and stop-work thresholds; provider billing permissions restricted | BR-17 |
| NFR-012 | Maintainable typed API, explicit errors, reproducible builds, reversible schema evolution | Must / MVP | OpenAPI reviewed, TypeScript/.NET build and lint pass during implementation; rollback rehearsal | BR-20 |

### 1.4 Traceability matrix

AC means the observable criterion in the requirements table; QA identifiers are in [security and QA](./security-and-qa.md). A row covers all listed IDs individually, not just the first item.

| Requirements | PRD feature | Components | Business rules | Phase | Acceptance / manual QA |
|---|---|---|---|---|---|
| FR-001, FR-002 | F-01 Publishing | WEB, API, DB | BR-01, BR-02 | P2 | Each FR AC; QA-01, QA-02 |
| FR-003 | F-02 Discovery | WEB, API, DB | BR-02 | P3 | FR-003 AC; QA-02, QA-20 |
| FR-004 | F-03 Assets | API, STORE | BR-03 | P4 | FR-004 AC; QA-03 |
| FR-005 | F-04 Pricing | WEB, API, DB | BR-04, BR-05 | P5 | FR-005 AC; QA-05 |
| FR-006 | F-05 Free fulfillment | API, DB, WORK, MAIL | BR-06, BR-10 | P7 | FR-006 AC; QA-04 |
| FR-007, FR-008 | F-06 QR request | WEB, API, DB | BR-07, BR-08 | P5, P6 | Each FR AC; QA-06, QA-10 |
| FR-009 | F-07 Verification | API, DB, WEB | BR-08 | P6 | FR-009 AC; QA-07, QA-08, QA-09 |
| FR-010, FR-011 | F-08 Secure delivery | API, DB, STORE, WORK, MAIL | BR-10, BR-11, BR-12 | P7 | Each FR AC; QA-11, QA-12, QA-13, QA-14 |
| FR-012, FR-013 | F-09 Contributions | WEB, API, DB, WORK, MAIL | BR-04, BR-06, BR-08 | P5, P6, P7 | Each FR AC; QA-05, QA-15 |
| FR-014, FR-017 | F-10 Operations/reports | API, DB, WEB, OBS | BR-09, BR-14, BR-16 | P8 | Each FR AC; QA-16, QA-21 |
| FR-015 | F-11 Settings | API, DB, WEB | BR-07, BR-15 | P2, P6 | FR-015 AC; QA-06, QA-17 |
| FR-016 | F-02 Discovery | WEB, API, OBS | BR-02, BR-16 | P3, P8 | FR-016 AC; QA-20 |
| FR-018 | F-12 Support/refunds | API, DB, WEB | BR-09 | P8 | FR-018 AC; QA-18 |
| FR-019 | F-13 Policies/UX | WEB | BR-13, BR-19 | P3 | FR-019 AC; QA-19 |
| FR-020 | F-14 Production operations | API, DB, STORE, WORK, MAIL, OBS | BR-15, BR-17 | P1, P9 | FR-020 AC; QA-17, QA-22, QA-23 |
| FR-021 | F-15 Future capabilities | Deferred | BR-18 | Future | No MVP implementation |
| NFR-001, NFR-010 | F-14 Production operations | WEB, API, DB, STORE | BR-15 | P1, P2, P9 | Each NFR evidence; QA-17 |
| NFR-002 | F-07 Verification | API, DB, WORK | BR-08, BR-12 | P6, P7 | NFR-002 evidence; QA-09, QA-12 |
| NFR-003 | F-03 Assets; F-08 Secure delivery | API, STORE | BR-03, BR-11 | P4, P7 | NFR-003 evidence; QA-03, QA-11 |
| NFR-004 | F-13 Policies/UX | WEB | BR-19 | P3, P9 | NFR-004 evidence; QA-19 |
| NFR-005, NFR-006 | F-02 Discovery | WEB, API, OBS | BR-16, BR-17 | P3, P9 | Each NFR evidence; QA-20 |
| NFR-007 | F-08 Secure delivery | WORK, DB, MAIL | BR-12 | P7, P9 | NFR-007 evidence; QA-12, QA-13 |
| NFR-008, NFR-011 | F-14 Production operations | OBS, DB, STORE | BR-17 | P1, P9 | Each NFR evidence; QA-22, QA-23 |
| NFR-009 | F-13 Policies/UX | API, DB, STORE, MAIL | BR-13 | P7, P9 | NFR-009 evidence; QA-21, QA-23 |
| NFR-012 | F-14 Production operations | WEB, API, DB | BR-20 | P1, P9 | NFR-012 evidence; QA-23 |
