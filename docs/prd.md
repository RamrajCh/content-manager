# Product Requirements Document

> Part of the [Technical Design Document](./technical-design-document.md). Stable requirement, feature, rule, phase, QA, and source identifiers are shared across this documentation set.

Product goals, user journeys, feature specifications, and UI requirements. See [requirements](./requirements.md) for acceptance criteria and [rules](./rules.md) for authoritative business behavior.

## 5. Product Requirements Document

### 5.1 Product definition

**Problem:** A single creator needs an inexpensive storefront for digital files, flexible amounts and optional contributions without a marketplace or an automated payment gateway.

**Goals:** discoverable public previews; honest free access; exact, auditable QR reconciliation; reliable email fulfillment; modest administrative workload; recoverable data and costs. **Non-goals:** multi-vendor, customer identity platform, LMS, streaming service, automated banking, tax engine, or universal free hosting.

**Users:** mobile-first Nepal visitors; customers paying NPR; one owner publishing and verifying. Customers need clear exact amounts, human-review expectations, an email correction/support route and accessible QR alternatives. Owner needs an actionable pending queue and financial/delivery separation.

**Success metrics, without invented baseline:** published products; public views; valid free requests; free request-to-provider-acceptance and first URL-issuance rates; checkout-to-submission and submission-to-confirmation rates; median/p95 verification delay; accepted/delivered/bounced email rates; verified optional contributions divided by eligible free sessions/requests (denominator stated); admin time/order; error/availability; storage/quota consumption. Browser counters are approximate and bot-influenced. File URL issuance is not a completed-download metric.

### 5.2 User journeys

| Journey / feature | Entry, actions, system behavior | Validation and success | Errors / recovery |
|---|---|---|---|
| J-01 Browse / F-02 | Home -> categories/search -> product preview | Published product metadata only, no login, usable keyboard/mobile layout | No matches has explicit empty state; API failure has retry, not fabricated empty results |
| J-02 Free / F-05 | Free product -> enter email -> accept request -> email access link | NotRequired payment, Eligible entitlement, one job; no QR/donation obligation | Typo/bounce handled by admin correction; quota outage shows queued delay |
| J-03 Fixed / F-04,F-06 | Product -> email -> server snapshot -> exact NPR QR -> customer pays externally -> submits reference | Fixed amount read server-side; Submitted is not paid | Price changes reconfirmed; wrong/partial amount reviewed, no access |
| J-04 Flexible / F-04 | PWYW or suggested product -> choose permissible amount or zero | Zero permitted by product grants free fulfillment; positive choice waits for verified funds | Below minimum/unsupported currency rejected with field error |
| J-05 Optional contribution / F-09 | Free product -> request free link; separately choose donation opportunity | Free order fulfilled regardless of donation outcome; positive donation uses separate QR request | Skip/cancel/reject contribution leaves free access intact |
| J-06 Platform support / F-09 | Enabled support page -> amount/email -> QR and reference | Confirmed donation records receipt, no asset entitlement | Disabled page/endpoint unavailable; reject notification includes support instructions |
| J-07 Approve / F-07 | Admin queue -> compare account statement, reference, amount/account -> approve | Revision/idempotency checked; one receipt and job | Duplicate reference or concurrent decision shows conflict, not second email/payment |
| J-08 Reject / F-07 | Admin finds missing/wrong funds -> reason -> reject | No paid entitlement; rejection notification; audit contains actor/time/reason | Customer uses support for correction/refund/replacement, not hidden state reset |
| J-09 Download / F-08 | Email -> access landing -> explicit exchange -> choose asset -> signed GET | Eligible pinned asset, current generation and expiry checked | Expired/revoked link explains support route without account enumeration |
| J-10 Publish / F-01,F-03 | Admin login/MFA -> draft fields/pricing -> upload/scan -> preview -> publish | Required fields, clean files, unique slug and valid pricing | Quarantine/type/slug errors block publish; stale edits return 412 |
| J-11 Operate / F-10,F-11,F-14 | Admin dashboard -> failures/quotas/settings/audit -> action | Pending financial and delivery queues distinct; reports match receipts | Dependency failure visible; backup/monitor failures treated as incidents |
| J-12 Refund/support / F-12 | Customer support -> owner validates purchase -> external refund/dispute handling -> record adjustment | Receipt immutable; full refund/relevant hold revokes access and old sessions | Partial refund cannot exceed verified net available amount; no invented automated payout |

### 5.3 Feature specifications

Each feature inherits the main/alternative/error flows of its J rows and the precise acceptance criterion of its linked FRs. Preconditions and dependencies below complete the feature contract; no feature is satisfied by UI alone.

| Feature | Purpose / user story | Preconditions and dependencies | Acceptance / scope |
|---|---|---|---|
| F-01 Publishing | As owner, control which products are discoverable | Admin session; category/slug validation; F-03 for publishing assets | FR-001/002; draft preview private; unpublish stops new checkout / MVP |
| F-02 Discovery/SEO | As visitor, find products and understand offers without login | Published products; public DTOs and cache invalidation | FR-003/016; public indexing only / MVP |
| F-03 Assets | As owner, attach clean private files | Admin, configured storage, trusted scan process | FR-004; invalid/oversize file blocked / MVP |
| F-04 Pricing | As customer, understand required and voluntary amounts | Valid versioned policy, safe money arithmetic | FR-005; all pricing rows server-enforced / MVP |
| F-05 Free fulfillment | As visitor, get permitted zero-price content without payment | F-03/04/08, delivery email | FR-006; no receipt/QR needed / MVP |
| F-06 QR request/status | As payer, see exact account/instructions and submit reference | Enabled validated account; F-04 | FR-007/008; refresh safe, status scoped / MVP |
| F-07 Verification | As owner, reconcile actual funds once | F-06, receiving statement access | FR-009; race/reject invariants / MVP |
| F-08 Secure delivery | As eligible customer, receive and use expiring links | F-03/05/07, worker/email | FR-010/011; retry/reissue independent from finance / MVP |
| F-09 Contributions | As supporter, contribute voluntarily | Valid donation settings; provider/legal confirmation for standalone | FR-012/013; no free-access coupling; support disabled initially / MVP |
| F-10 Reports | As owner, see accurate financial/operational counts | Orders/receipts; event privacy controls | FR-014/017; pending amounts not revenue / MVP |
| F-11 Settings | As owner, safely change QR and operational policy | Admin, versioned validation/audit | FR-015; snapshots preserved / MVP |
| F-12 Support/refunds | As owner, resolve email, disputes, and external refunds | Verified purchase/ownership checks; F-07/08 | FR-018; append-only receipt adjustments / MVP |
| F-13 Policies/UX | As visitor, understand terms and complete accessible flows | Owner-approved policies and responsive components | FR-019; NFR-004/009 manual evidence / MVP |
| F-14 Production operations | As owner, detect/recover failures and release safely | All launch dependencies and approved hosting decisions | FR-020 and operational NFRs; restore/review evidence / MVP |
| F-15 Future capabilities | Extend product only after explicit reprioritization | Separate requirements/design revision | FR-021; no hidden MVP dependencies / Future |

### 5.4 UI requirements

Public: home/catalog/category/search; preview detail with file formats/sizes, pricing and license/support; amount/email form; QR instructions with exact text alternative and receiving name; reference submission; request status; access/download confirmation; policies. Do not call Submitted "Payment successful". Use "Submitted for manual review"; no promised verification duration until owner sets support expectations.

Admin: MFA login; dashboard; product/editor/preview; categories/tags; pricing; asset scan/attachment state; pending verification queue; order detail and reference review; donation/transaction reports; delivery status/retry/reissue; settings/audit/support/refund views. Show required price, contribution, observed amount and exact receiving account side by side. Require reason for reject, email correction, refund, reference release and revocation.

Shared: touch targets, visible focus, explicit labels and errors linked to fields, semantic tables, text alternative to QR, no color-only state, skeleton/loading and retry states, clear empty states, progress for uploads, non-duplicating disabled-in-flight submissions. Responsive layout at 320px and desktop; 200% zoom; amounts never rely solely on locale punctuation. Restore form non-secret inputs after recoverable errors; never put email/reference/token into analytics URLs.
