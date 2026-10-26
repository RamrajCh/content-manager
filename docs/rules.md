# Business and Technical Rules

> Part of the [Technical Design Document](./technical-design-document.md). Stable requirement, feature, rule, phase, QA, and source identifiers are shared across this documentation set.

Authoritative content, pricing, payment, access, privacy, security, and operational rules. See [designs](./designs.md) for contracts and workflows.

## 6. Business rules and technical rules

### 6.1 Content lifecycle

**BR-01. Publishing validation:** required title, reserved unique normalized slug, sanitized non-empty description/summary, owner-approved versioned license, valid pricing and 1-5 validated private assets. At most 3 categories and 10 tags (assumptions). Covers optional but use safe placeholder. Product, pricing, category and slug edits use revision checks.

**BR-02. Visibility:** only Published products appear in public catalog/search/sitemap. Draft preview requires admin auth. Unpublished/Archived stop new requests; existing eligible fulfillment remains pinned to prior assets unless explicitly recalled. Hard-delete only unreferenced drafts; otherwise archive. Permanent slug reservations prevent mistaken links.

**BR-03. Assets:** 100,000,000-byte per-file cap, quarantine before validation, content/type/checksum/scan controls, no private public bucket. Never replace bytes under an already sold asset ID. Review/delete orphan uploads and track quota.

### 6.2 Pricing, access, and donation matrix

Every product has exactly one base mode; Suggested is PWYW behavior with a required displayed suggestion, not a second payment engine. All rows include public preview. Email required only to request fulfillment/contribution receipt, not browsing. A positive payable order always requires verification even if its product also permits a separate zero path.

| Mode | Zero / minimum | Customer display and amount | Access trigger | Recording |
|---|---|---|---|---|
| Free | Zero only | "Free"; optional separate support link | Valid zero request | NotRequired, no receipt |
| Fixed | No zero; fixed >0 | Exact required NPR amount | Confirm exact funds | Required amount receipt |
| PWYW | `allowZero`; if nonzero, >=configured minimum and A-06 floor | "Choose your price"; optional suggestion | Zero request immediate; chosen positive amount confirmed | Positive choice recorded as required amount for that order |
| Suggested | Same allowed range as PWYW; suggestion required and within positive range | "Suggested NPR X; choose an allowed amount", zero option explicit if allowed | Same as PWYW | Suggestion snapshot plus chosen required amount |
| OptionalDonation | Free access, donation independent | "Get free link" and separate "Support" action | Free request immediate regardless of donation | Free order plus separate positive donation order if selected |
| FixedPlusDonation | Required fixed >0; top-up >=0 | Required price and optional top-up shown separately | Confirm total chosen payable amount | Required and contribution components separately stored |

**BR-04. Mode validation:** Free/OptionalDonation cannot encode a positive required price and require allowZero=true. Fixed/FixedPlusDonation require positive fixedPaisa, cannot allow zero. contributionEnabled may be true for Free, must be true for OptionalDonation, and must be false for other modes; FixedPlusDonation uses its own itemized top-up. PWYW/Suggested minimum, allowZero and suggested settings must agree: positive minimum >=100 paisa; explicit zero exception is allowed when allowZero is true. Minimum zero is represented by allowZero, not a fractional positive payment below the floor. Suggested value must be positive and >=positive minimum. Reject irrelevant/contradictory config fields.

**BR-05. Money:** all NPR values stored/calculated as integer paisa; accept up to 2 decimal places in human input, reject excess precision rather than silently round. Currency must equal NPR. Max total 10,000,000 paisa; nonzero floor 100 paisa. No comparison tolerance. Format using decimal-safe parsing; never `float` money calculations. QR/payment account precision, accepted amounts, fees/tax treatment require provider/accountant confirmation; do not add invented fees/taxes.

**BR-06. Donation independence:** optional free-product/platform support uses a separate order with `kind:SupportDonation`, optional relatedProductId metadata, no asset entitlement. Skipping/rejection does not affect free order. Fixed top-up is part of chosen total but separately itemized. PWYW chosen amount is not automatically a tax/legal "donation". `standaloneDonationsEnabled` defaults off and gates only requests without relatedProductId; a product-linked optional contribution requires a Published Free/OptionalDonation product with its contribution opportunity enabled. Related-product attribution never grants access. Marketing consent is never implied.

### 6.3 Payment verification and financial rules

**BR-07. Request creation:** check an existing idempotency key/payload match before new-order validation; otherwise server validates current Published product, policy version, email, safe amounts, assets and enabled account for positive amounts and snapshots terms/assets/account instructions. Idempotency payload hash includes relevant identity/pricing fields. Response replay lasts 24h encrypted; later same key returns 409 `IDEMPOTENCY_REPLAY_EXPIRED` with existingOrderId and safe recovery instructions, never another order or a new access secret. Current status needs the original order credential or admin support; free recipients may also use their already emailed link. Zero requests have no receiving-account dependency.

**BR-08. Evidence and decision:** reference is a claim, never proof. Admin checks actual credited funds, receiving account, currency, unique transaction identifier and exact selected total. Submissions/repeated decisions enforce revision/idempotency/unique reference; decision audit includes actor/time/reason and comparison. Underpayments and overpayments are not auto-accepted, no hidden proportional access or silent conversion to donation. Request external correction/refund; if appropriate create explicitly accepted replacement order and reconcile funds there. No split/partial settlement in MVP. Provider must expose reliable account transaction IDs before launch; otherwise redesign evidence handling.

**BR-09. Refunds/disputes:** no API transfers money. Owner resolves external refund/dispute, appends amount/reference/reason/actor and maintains immutable original receipt. Full refund revokes entitlement and future grant/URL issuance; partial refund retains access by default and is reported separately; owner can security-revoke with reason. Open dispute holds access; resolution restores only with explicit audited decision. Existing 60-second URLs/ongoing transfers cannot be recalled. Prevent total refunded exceeding receipt; duplicate refund external reference or command cannot append twice. Legal refund/tax/retention obligations require Nepal-qualified advice.

### 6.4 Access, email, and privacy rules

**BR-10. Entitlement:** free/zero acceptance or positive verified funds is the only ordinary eligibility event; receipt alone for support donation grants no assets. No delivery eligibility from screenshot, client state, pending submission, or email provider event.

**BR-11. Secure links:** token hash, expiry, order entitlement, current generation, asset binding and revocation checked server-side; API issues only short presigned URLs to authorized capability holders. No permanent public file paths. Reissue is independent from payment; no promise of DRM or identity-bound links.

**BR-12. Reliability:** queued intent persists with eligibility. Provider Accepted/Delivered/Failed is separate from funds; retries use stable payload/provider key. Unknown outcomes reconcile before resend outside idempotency retention. Bounces visible, no swallowed errors. Every admin resend/recipient change increments generation and invalidates old access.

**BR-13. Privacy:** store delivery email encrypted and keyed hash for exact support search, reference encrypted plus keyed hash for uniqueness, not raw statement screenshots. No marketing in MVP. Proposed retention: unsubmitted/rejected/expired orders and related personal data 90 days after closure; financial confirmed receipts/reference metadata 1 year pending legal advice; email job logs 30 days after terminal outcome; security/audit records 1 year; raw redacted app logs 14 days; aggregate counters 13 months. Confirmed order entitlement/pinned asset/support data may remain while fulfillment commitment exists; do not erase email then promise indefinite automated resend. Owner must set access-support term before launch. On deletion/anonymization retain only legally justified records; explain immutable backup purge lag (up to 30 days) and reapply deletion ledger after restore. Retention assumptions are not statutory claims.

### 6.5 Administration, security, and analytics rules

**BR-14. Reporting:** revenue/received funds derive solely from immutable confirmed receipts; group required amount, voluntary components, refunds, net funds and count separately. Show chosen pending amounts as outstanding, not cash. Reporting day uses Asia/Kathmandu; store timestamps UTC and convert consistently. CSV neutralizes formula prefixes and enforces admin authorization. Counter analytics is not accounting.

**BR-15. Security:** admin password hashed with ASP.NET supported password hasher and calibrated parameters, MFA TOTP/recovery hashes, no default credentials, no public signup. Admin cookie `__Host-admin` Secure, HttpOnly, Path=/, SameSite=Strict, no Domain. Proposed 30-minute idle/8-hour absolute expiry; login failures throttled, recovery offline and audited. Rotate sessions after login and sensitive recovery. Same-origin antiforgery token on cookie-authenticated unsafe methods, origin checks and restrictive CORS. Server authorization per endpoint/resource. Encode outputs, sanitize rich text via maintained allowlist, parameterize database queries; never evaluate user regex/scripts.

HTTPS, HSTS after TLS validation, restrictive CSP/frame-ancestors, nosniff, no-referrer on sensitive routes, no-store on auth/order/download responses. Reject unsafe redirects. Rate-limit provisionally: order create 5/IP/10min and 3/email/15min using keyed email hash; submit 5/order/10min; access exchange 20/IP/10min; URL issue 10/session/min; admin login 5 failures/15min with owner recovery path. Store counters durably or enforce at ingress across instances; in-memory-only limiter is insufficient after restarts/scale. Tune for shared Nepal mobile networks and provide support; no hidden forced signup. Cap JSON bodies at 100KB (product description fits); enforce explicit upload byte limits separately.

Never log passwords, MFA seeds/codes, cookies, authorization headers, email/ref tokens, signed URLs, plaintext email or payment reference. Redact at proxy, application, error tracker, provider logs where configurable. Audit viewing/exporting sensitive admin data. Separate credentials per environment/service, rotate at incident, restrict DB network access to stable host egress where available; inability to enforce safe connectivity is a deployment blocker, not justification for unauthenticated/public DB exposure.

**BR-16. SEO/analytics:** metadata, canonical links, social cards and sitemap only from Published products. Robots/noindex are indexing hints, not access control. Public beacon excludes access/request/checkout/admin URLs and sensitive query strings. First-party events are fixed enum counters without free-text fields; approximate funnel spam mitigated by rate limits. Verified payments/refunds/accepted deliveries derive from durable records. Privacy notice must describe analytics; do not repeat vendor "no consent needed" claims as Nepal legal advice.

### 6.6 Operational and engineering rules

**BR-17. Operations:** owner is incident responder and backup custodian. Alert on repeated 5xx/readiness failure, any unexpected payment/access inconsistency, due delivery oldest age >5min on active host, permanent bounce/failure, backup age >24h, and quotas >70% warning/>90% action (proposed thresholds). Monitor storage including quarantine/retained files/backups, email daily and monthly consumption, DB storage/network/connections, host hours/builds/egress and budget. Thresholds cannot prevent all charges. At action threshold pause new large uploads/nonessential emails and positive checkouts where fulfillment is at risk, never alter verified receipts.

Daily encrypted database exports with index definitions and schema/config versions; retain 7 daily and 4 weekly copies, max 30-day retention, plus independent encrypted offline copy. For consistent free-cluster dumps, pause **all** writers including worker/webhooks/settings during export and record queued external notifications for later reconciliation; if quiescence cannot be guaranteed, adopt a supported snapshot strategy before claiming point-in-time consistency. Resume safely. Keep originals and every sold asset in backup manifest; private object replication alone is not independent deletion protection.

On new asset publication back up immutable bytes/checksum before enabling sale. Daily incremental copy/check, periodic full manifest integrity check. Separate backup delete credentials and encrypt with recovery key not stored only on application host. Daily export copies can exceed free storage; [costs and providers](./costs-and-providers.md) budgets are limits, not promises.

Restore drill before launch and after material changes: isolate target environment, restore DB/indexes/keys and asset manifest, verify hashes, disable actual email, replay deletion ledger, validate known zero/paid/rejected orders and revocations, reconcile funds/email events newer than restore point, then approve cutover. Measure RPO/RTO, do not merely copy files. Real incidents: stop unsafe checkout, preserve evidence/redacted logs, revoke compromised keys/sessions/grants, reconcile receiving account and queued sends, restore/rollback, communicate impact and record post-incident action.

**BR-18. Scope control:** no future capability enters MVP through an API/model assumption. Revisit TDD and threat model before adding accounts, courses, streaming, gateways, multi-currency or public evidence uploads.

**BR-19. Manual quality gate:** perform [security and QA cases](./security-and-qa.md#92-manual-qa-matrix) for each critical release, desktop and mobile. Absence of required automated unit/integration tests is accepted initial scope, not absence of review/build/lint or permission to skip security. Record residual regression/race risk; prioritize later automated money/state/access/outbox tests.

**BR-20. Engineering conventions:** meaningful feature/resource names; C# PascalCase/types and camelCase JSON/TypeScript consistent with tooling; strict DTOs and no unsafe casts to bypass validation; Problem Details with correlation IDs; explicit expected failure handling; central redaction; additive schemas; timeouts/cancellation; pinned maintained dependencies and patch cadence at least monthly, urgent exploitable fixes promptly. Review changes to payment/auth/grants by a qualified reviewer before launch. If owner is sole implementer, obtain external targeted review rather than claiming independent self-review.
