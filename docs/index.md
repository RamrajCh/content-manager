---
okf_version: "0.2"
---

# Content Manager Knowledge Bundle

This directory follows [Open Knowledge Format (OKF) v0.2](https://github.com/GoogleCloudPlatform/open-knowledge-format/blob/main/SPEC.md).
It describes a single-owner digital-download platform, its design baseline,
implementation plan, and recorded acceptance evidence. It is not a
production-readiness certification.

## Overview

- [Technical Design Document](./technical-design-document.md) - Design baseline, confirmed constraints, assumptions, and glossary for the digital-download platform.

## Product and requirements

- [Product Requirements Document](./prd.md) - Product goals, user journeys, feature specifications, and UI requirements.
- [Requirements and Traceability](./requirements.md) - Actors, functional and non-functional requirements, acceptance criteria, and traceability.
- [Business and Technical Rules](./rules.md) - Authoritative content, pricing, payment, access, privacy, security, and operational rules.

## Architecture and design

- [Architecture and Technical Stack](./architecture.md) - Technology choices, system boundaries, deployment architecture, and rollback specifications.
- [Detailed Technical Designs](./designs.md) - Frontend routes, data models, REST contracts, payment and delivery states, storage, and email workflows.

## Delivery and implementation evidence

- [Development Phases](./phases.md) - Implementation dependencies, phase deliverables, manual acceptance gates, and deferred scope.
- [MVP Execution Plan](./execution-plan.md) - Implementation work packages, dependencies, acceptance evidence, milestones, and launch stop conditions.
- [Phase 1 Implementation Evidence](./phase-1-progress.md) - Recorded local-foundation implementation evidence, validation results, and outstanding acceptance gates.
- [Phase 2 Implementation Evidence](./phase-2-progress.md) - Recorded administrator and publishing implementation evidence, owner acceptance, and outstanding manual checks.

## Operations, risks, and research

- [Security, Manual QA, and Production Readiness](./security-and-qa.md) - Design threat controls, manual QA cases, and production-launch requirements.
- [Risks, Dependencies, and Open Decisions](./risks-and-decisions.md) - Risk register, dependencies, and unresolved launch decisions.
- [Costs, Providers, and Nepal Availability](./costs-and-providers.md) - Provider research, free-tier constraints, Nepal availability, capacity estimates, and adoption checks.
- [References and Evidence Notes](./references.md) - Official research sources, stable citation identifiers, and evidence qualifications.

## Bundle maintenance

- [Update log](./log.md) - Dated changes to this knowledge bundle.

Each non-reserved Markdown file is a concept with YAML frontmatter containing
`type`, `title`, `description`, and `tags`. Its concept ID is its path relative
to this directory without `.md`. Existing filenames, section numbers,
requirement identifiers, and relative links are retained.

When adding a concept, include a non-empty `type`, give it a descriptive title
and one-sentence description, and list it here using that description. Use
standard relative Markdown links for relationships so links also work in
repository viewers. Keep each subject's authoritative content in its existing
concept rather than duplicating it in the index.

`index.md` and `log.md` are reserved navigation and history files, not concepts.
Only this root index declares `okf_version`; other index files, if added, have
no frontmatter. Record changes in the log under newest-first `YYYY-MM-DD`
headings. Optional provenance or verification metadata must describe actual
evidence; format conversion alone does not establish verification.
