# Residential Building Management — Solution Design

**Key assumptions:** one pilot association with one building and 60 units; a delivery team familiar with web applications; managed regional hosting; no confirmed existing identity, finance or messaging integrations; English UI first; jurisdiction and legal obligations unconfirmed. These are proposed planning inputs, not discovered facts.

**Tenant decision:** one SaaS tenant represents one association or management organization that controls its own data and may contain multiple buildings. A management company acting for independent associations uses separate tenants and can have administrator memberships in each. “Tenant” never means a person renting an apartment.

**MVP1 outcome:** a real resident accepts an invitation, signs in, reports a private issue, exchanges comments with an administrator, receives a resolution, and confirms closure or reopens it. The administrator has a current unit register, a scoped work queue and a complete history. Launch includes genuine tenant isolation, access revocation, audit, restore-tested backups, monitoring and safe deployment.

**Status:** proposed implementation-ready baseline, dated 7 September 2026. This is a specification, not application code, a deployed service, legal advice, a certification or evidence of production testing. All deliverables are English Markdown; all diagrams are Mermaid. See [validation.md](validation.md) for actual documentation checks and renderer results.

## Read the documentation

| Document | Authority |
| --- | --- |
| [solution-design.md](solution-design.md) | Business context, scope classification, architecture, stack, authorization matrix, UX and NFRs |
| [shared-patterns.md](shared-patterns.md) | Authoritative authorization, transaction, error, file and delivery contracts |
| [data-model.md](data-model.md) | Entity ownership, constraints, ER views and financial rules |
| [api-catalog.md](api-catalog.md) | Use-case-mapped operations, common API conventions and representative contracts |
| [use-case-catalog.md](use-case-catalog.md) | Complete 55-use-case baseline and release allocation |
| [use-cases/](use-case-catalog.md) | One dedicated 14-section specification and end-to-end sequence per catalogue entry |
| [mvp-roadmap.md](mvp-roadmap.md) | Five production increments, MVP comparison, backlog, gates, effort and manual alternatives |
| [operations-and-testing.md](operations-and-testing.md) | Test design, environments, cutover, ownership and operational runbooks |
| [decisions.md](decisions.md) | ADRs, risk register, questions and deferred decisions |
| [traceability.md](traceability.md) | Requirement, use-case, API, screen, entity and acceptance-test coverage |
| [sources.md](sources.md) | Verified primary references and limits of external verification |

Shared definitions override shorthand in individual use cases. Conflicts must be fixed before implementation, not resolved differently by each team. Future specifications define contracts for later releases; they do not authorize adding future tables, jobs or services to MVP1.

## Requirement status and assumptions

**Confirmed (C):** multiple buildings and tenants; Building Administrator and Resident roles; secure tenant isolation; complete declared-scope use cases with detailed Mermaid sequences; smallest usable production MVP; incremental delivery; no application implementation code. **Assumed (A):** the included business rules and workloads below. **Recommended (R):** architectural and delivery choices. **Optional included (O+):** explicitly selected later capabilities. **Optional excluded (O−):** not in this baseline; require a scope decision before implementation. **Excluded (X):** outside this design.

| ID | Proposed input | Consequence and validation owner |
| --- | --- | --- |
| A-01 | Association/controller is tenancy boundary; independent associations stay separate | Product owner confirms contractual data boundary before schema work |
| A-02 | MVP1 pilot: 1 building, 60 units, 100 invited residents, 2 administrators; production supports many tenants | Manual register entry is viable; test with 2 independent tenants before pilot |
| A-03 | First-year envelope: 50 tenants, 100 buildings, 10000 units, 15000 identities, 250 concurrent sessions, 25 requests/s sustained and 100/s burst | Capacity benchmark is a proposed gate, not an observed workload |
| A-04 | 3 product engineers; part-time architect, QA, platform and product owner; budget unspecified | Estimates use person-weeks, no monetary promise; procurement may delay launch |
| A-05 | Managed single-region container service and PostgreSQL, managed OIDC and transactional email available | Validate data residency, recovery, contracts and actual price before committing provider |
| A-06 | English interface first, Unicode names/text, one tenant IANA zone; later translations are a separate content effort | Pilot must understand English or approve translation before launch |
| A-07 | One ISO currency per tenant; liability and allocation rules need written approval before MVP3 | Finance cannot ship from architectural assumptions alone |
| A-08 | Issues are ordinary service requests, not emergency dispatch, legal case management or guaranteed repair SLAs | Show clear emergency contact routing; service expectations set by association |
| A-09 | Jurisdiction, controller/processor roles, legal retention, electronic voting rules and financial reporting duties unknown | Legal/privacy specialist validates before real personal data; formal voting excluded |
| A-10 | No enterprise Kubernetes, identity provider, SMS contract or accounting platform is established | Do not assume shared corporate infrastructure; choose minimal managed services |
| A-11 | Ordinary resident issue visibility is reporter plus authorized administrators | Co-occupants do not see each other complaints; explicit broader collaboration deferred |
| A-12 | Baseline supports ordinary exclusive facility bookings and advisory polls only | No booking payments, formal quorum, delegated votes or secret-ballot promise |

Questions and owners are recorded in [decisions.md](decisions.md). No clarification response is required to use this documentation as the proposed baseline.
