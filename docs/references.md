# References and Evidence Notes

> Part of the [Technical Design Document](./technical-design-document.md). Stable requirement, feature, rule, phase, QA, and source identifiers are shared across this documentation set.

Shared official sources cited using stable S identifiers across this documentation set.

## 11. References and evidence notes

All external references are official provider/framework sources, reviewed 2026-10-26. Links not successfully retrieved are not used as evidence. The supplied product brief and subsequent owner clarifications are the product sources; no pre-existing repository documentation/application was present.

| ID | Official source | Evidence used / qualification |
|---|---|---|
| <a id="s-01"></a>S-01 | [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | .NET 10 LTS through 2028-11-14; .NET 8/9 end 2026-11-10; recheck patches before build |
| <a id="s-02"></a>S-02 | [MongoDB write atomicity](https://www.mongodb.com/docs/manual/core/write-operations-atomicity/) | Single-document atomic updates and expected-value filters |
| <a id="s-03"></a>S-03 | [Next.js self-hosting](https://nextjs.org/docs/app/guides/self-hosting) | Node/container compatibility, reverse proxy, caching/environment considerations |
| <a id="s-04"></a>S-04 | [Render Docker](https://render.com/docs/docker) | Container deploy compatibility for .NET, not native .NET runtime assumption |
| <a id="s-05"></a>S-05 | [Cloudflare Pages Next.js guide](https://developers.cloudflare.com/pages/framework-guides/nextjs/) | Static Pages versus dynamic Workers deployment; adapter compatibility not independently exercised |
| <a id="s-06"></a>S-06 | [Workers pricing](https://developers.cloudflare.com/workers/platform/pricing/) | Free CPU/request limits, paid minimum; no deployment guarantee |
| <a id="s-07"></a>S-07 | [Vercel Hobby](https://vercel.com/docs/plans/hobby) | Commercial-use restriction explicitly excludes this free storefront deployment |
| <a id="s-08"></a>S-08 | [Cosmos DB free tier](https://learn.microsoft.com/en-us/azure/cosmos-db/free-tier) | 1,000 RU/s, 25GB, creation-time opt-in, no serverless, overage billing |
| <a id="s-09"></a>S-09 | [Cosmos transactional batch](https://learn.microsoft.com/en-us/azure/cosmos-db/transactional-batch) | ACID batch within same logical partition, not arbitrary cross-partition transaction |
| <a id="s-10"></a>S-10 | [R2 pricing](https://developers.cloudflare.com/r2/pricing/) | Standard allowance/rates/rounding and free internet egress |
| <a id="s-11"></a>S-11 | [R2 presigned URLs](https://developers.cloudflare.com/r2/api/s3/presigned-urls/) | GET/PUT and bearer expiry semantics, S3 compatibility |
| <a id="s-12"></a>S-12 | [AWS S3 pricing](https://aws.amazon.com/s3/pricing/) | Region/feature-based billing; retrieved content insufficient for target-region free-tier quote |
| <a id="s-13"></a>S-13 | [Resend pricing](https://resend.com/pricing) | Free monthly/daily quota and cited paid example; not Nepal eligibility evidence |
| <a id="s-14"></a>S-14 | [Resend idempotency](https://resend.com/docs/dashboard/emails/idempotency-keys) | Stable payload/key and 24-hour retention |
| <a id="s-15"></a>S-15 | [Brevo Free limits](https://help.brevo.com/hc/en-us/articles/208580669-FAQs-What-are-the-limits-of-the-Free-plan) | 300 daily sends, queue/branding, no time limit |
| <a id="s-16"></a>S-16 | [Cloudflare Web Analytics](https://developers.cloudflare.com/web-analytics/) | Page-performance analytics available on all plans; no arbitrary funnel-event capability assumed |
| <a id="s-17"></a>S-17 | [Plausible self-hosting](https://plausible.io/self-hosted-web-analytics) | CE software versus managed service and operational burden; vendor legal/consent claims not adopted |
| <a id="s-18"></a>S-18 | [Render Free services](https://render.com/docs/free) | Sleep, ephemeral disk, shared hours, quota suspension, SMTP restrictions; vendor production warning |
| <a id="s-19"></a>S-19 | [Render regions](https://render.com/docs/regions) | Singapore available; not Nepal account eligibility |
| <a id="s-20"></a>S-20 | [Azure Container Apps pricing](https://azure.microsoft.com/en-us/pricing/details/container-apps/) | Monthly consumption grants, zero-scale, ancillary costs not quoted |
| <a id="s-21"></a>S-21 | [Cloudflare Pages limits](https://developers.cloudflare.com/pages/platform/limits/) | Builds/files/25MiB static asset cap |
| <a id="s-22"></a>S-22 | [Atlas Free limits](https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/) | Rendered browser table used because text extraction was incomplete; 0.5GB/100 ops/s/500 connections/transfer limits and backup statement |
| <a id="s-23"></a>S-23 | [Deploy Atlas Free](https://www.mongodb.com/docs/atlas/tutorial/deploy-free-tier-cluster/) | One free cluster/project, selected-region availability, small proof-of-concept positioning |
| <a id="s-24"></a>S-24 | [Atlas AWS regions](https://www.mongodb.com/docs/atlas/reference/amazon-aws/) | Regional compatibility varies by tier; extracted matrix incomplete, selected free region unverified |
| <a id="s-25"></a>S-25 | [Atlas backup overview](https://www.mongodb.com/docs/atlas/backup-restore-cluster/) | Says Free managed backups unavailable; conflicts with S-22, so independent exports required |
| <a id="s-26"></a>S-26 | [R2 getting started](https://developers.cloudflare.com/r2/get-started/) | Subscription checkout required; free allowance is not no-billing-account proof |
| <a id="s-27"></a>S-27 | [R2 data location](https://developers.cloudflare.com/r2/reference/data-location/) | APAC hint is best-effort, not residency guarantee |
| <a id="s-28"></a>S-28 | [Resend verified domains](https://resend.com/docs/dashboard/domains/introduction) | Owned sending domain/DNS verification required |
