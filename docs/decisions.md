# Decisions, risks and stakeholder validation

[Architecture](solution-design.md) · [Roadmap](mvp-roadmap.md)

ADRs are proposed decisions accepted for this design baseline, pending named stakeholder validation before the relevant release. No unverified provider or legal assumption is silently treated as fact.

## ADR-001 — Tenant boundary

**Context:** administrators can manage many buildings and users can belong to many organizations. **Options:** tenant per building; tenant per organization. **Decision:** one association/data-controlling organization per SaaS tenant, buildings inside; independent associations remain separate even with common management company. **Rationale:** stable contractual/control boundary and multi-building administration. **Consequences:** building/unit authorization remains essential inside a tenant; no cross-tenant person master. **Revisit:** legal control boundary or portfolio consolidation requirement proves different. **Owner:** product/controller representative. Validate before first schema.

## ADR-002 — Shared schema isolation

**Context:** many small tenants, small operations team. **Options:** shared schema/RLS; schema per tenant; database per tenant. **Decision:** shared PostgreSQL schema with non-null tenant IDs, composite FKs, explicit backend policies and FORCE RLS, least-privilege non-owner runtime. **Rationale:** one migration and low per-tenant overhead. **Consequences:** shared blast radius and more complex single-tenant restore; test pool context and privileged role boundaries. **Revisit:** contractual physical separation, large-tenant noisy-neighbor load, per-tenant restore SLA. **Owner:** architect/security/database lead. Evidence: [PostgreSQL row security](https://www.postgresql.org/docs/current/ddl-rowsecurity.html).

## ADR-003 — Identity and server session

**Context:** secure accounts/recovery should not consume most delivery effort. **Options:** custom password store; self-hosted IdP; managed OIDC. **Decision:** managed OIDC with code/PKCE and server-side BFF session, tenant grants in application database. **Rationale:** reuse MFA/recovery and keep tokens out of browser. **Consequences:** provider procurement/MAU/residency dependency, assurance-claim contract testing; local logout committed before attempted provider logout. **Revisit:** legal residency, pricing, portability or operating capability favors self-hosted Keycloak. **Owner:** security/platform. Provider logout mechanism reference: [OpenID RP-Initiated Logout](https://openid.net/specs/openid-connect-rpinitiated-1_0.html).

## ADR-004 — Modular monolith first

**Context:** tight delivery capacity and transactional workflows. **Options:** modular monolith, microservices, serverless function mesh. **Decision:** one application artifact and database, logical module ownership, hosted worker initially. **Rationale:** fastest coherent delivery with fewer deployment/failure modes. **Consequences:** modular boundaries enforced in code review/tests; module scaling shared until measured need. **Revisit:** independent teams/release cadence, regulatory separation or workload mismatch after tuning. **Owner:** engineering lead.

## ADR-005 — Outbox and at-least-once side effects

**Context:** business save must survive email outages and retries. **Options:** synchronous provider call inside transaction; fire-and-forget task; broker plus distributed services; PostgreSQL outbox/leases. **Decision:** domain/audit/outbox commit together, hosted worker sends after commit; delivery can duplicate and unknown outcomes reconcile. **Rationale:** durable intent without extra infrastructure. **Consequences:** lease/retry/deduplication/queue monitoring are required MVP1; no exactly-once provider promise. **Revisit:** sustained throughput or integration fan-out exceeds fair DB polling capacity. **Owner:** backend/platform.

## ADR-006 — Managed single-region hosting

**Context:** no established enterprise runtime, budget unknown. **Options:** managed app/database; self-managed VMs; Kubernetes. **Decision:** approved-region managed always-on container hosting, managed PostgreSQL/secrets/logs/backups; one app initially under 99.5% proposal. **Rationale:** low operations burden and quick recoverable deployment. **Consequences:** provider-specific IAM/restore details and contracts need verification; a single app is not highly available. **Revisit:** measured SLO miss, higher availability contract, residency or proven lower managed cost at scale. **Owner:** platform/product. Region and provider are pre-implementation validation items, not hidden assumptions.

## ADR-007 — Issue slice before finance

**Context:** useful first release without unresolved accounting migration. **Options:** notice board, issue resolution, financial transparency. **Decision:** text-only private issue loop in MVP1, files/communication MVP2, finance MVP3. **Rationale:** two-role value and measurable outcome with fewer policy/data dependencies. **Consequences:** administrators keep established finance/contractor processes; attachments cannot use unsafe public links as workaround. **Revisit:** pilot interviews show finance is the only adoption driver and reconciled data/policy already exists. **Owner:** product owner.

## ADR-008 — Private backend file delivery

**Context:** former residents must lose current content access promptly. **Options:** public links, presigned downloads, authenticated backend stream. **Decision:** backend stream after current parent-resource authorization; quarantine/scan before visibility. **Rationale:** request-time membership enforcement, bounded file sizes make proxy practical. **Consequences:** bandwidth/app capacity overhead, no recalled downloaded bytes; object/metadata reconciliation needed. **Revisit:** measured scale requires short-lived signed links and approved revocation-delay trade-off. **Owner:** security/files lead.

## ADR-009 — Immutable receivables subledger

**Context:** posted money must be auditable and ownership transfer must not leak debt. **Options:** editable unit balance CRUD; immutable event/journal model; full accounting suite integration. **Decision:** tenant-currency liability accounts, balanced journals, immutable postings, allocation history and compensating corrections; recording external payments only. **Rationale:** reproducible balances without building a full accounting system. **Consequences:** accountant-approved policies and opening reconciliation are hard gates; richer finance is a later release. **Revisit:** statutory ledger/payment processing mandate or existing accounting API is established. **Owner:** finance architect/accountant.

## ADR-010 — Advisory community governance only

**Context:** legal voting rules unknown. **Options:** binding votes; advisory polls; exclude all polls. **Decision:** one-membership advisory poll, explicit eligibility snapshot/current access, aggregates after close; no statutory quorum claim. **Rationale:** useful feedback within an honest evidentiary boundary. **Consequences:** formal resolutions remain in approved external process; ballot linkage is restricted but no secret-ballot guarantee. **Revisit:** specialist-approved jurisdictional voting specification and budget. **Owner:** association/controller.

## ADR-011 — Responsive SPA and .NET backend

**Context:** transactional authenticated app with no SEO requirement. **Options:** React static SPA + BFF; Next.js server-rendered app; all-server-rendered framework. **Decision:** React/TypeScript assets served same origin by ASP.NET Core .NET 10 LTS backend. **Rationale:** one runtime and clear server session boundary. **Consequences:** assumes team .NET/TypeScript competence; exact EF/Npgsql/React package versions/compatibility and licenses checked in initial spike. **Revisit:** actual team skills, accessibility implementation cost or proven SSR need. **Owner:** engineering lead.

## Risk register

| ID | Risk / consequence | Likelihood-impact hypothesis | Mitigation / trigger / owner |
| --- | --- | --- | --- |
| R-01 | Wrong tenant/controller boundary causes expensive data redesign | Medium/high | Validate A-01 before schema; product/controller |
| R-02 | Authorization implemented as UI filter leaks neighbor/tenant data | Medium/critical | EN-002 and SEC matrix; block launch on any leak; security |
| R-03 | Pilot admin fails to respond; portal loses adoption | Medium/high | Daily queue owner/backup, response metrics and training; product |
| R-04 | Poor register/contact data invites wrong people | High/high | Verified sources, scoped preview, no auto-import grants; steward |
| R-05 | Managed IdP lacks required MFA assurance or region | Medium/high | Protocol/procurement spike before stack lock; security/platform |
| R-06 | Backups exist but cannot restore keys/files consistently | Medium/critical | Timed isolated drill and tombstone replay; platform |
| R-07 | Email acceptance mistaken for delivery/read | High/medium | Explicit statuses and Unknown reconciliation; integration lead |
| R-08 | Finance liability/openings unapproved | High/high | Hard gate before MVP3; no guessed balances; finance owner |
| R-09 | Posted corrections implemented as edits | Medium/critical | Immutable journals and property tests; finance lead |
| R-10 | Uploads expose malicious or stale-authorized content | Medium/high | Quarantine/scan and authenticated stream; FILE tests |
| R-11 | One large tenant monopolizes jobs/database | Medium/medium | Fair claims, caps and load metrics; scale trigger by SLO |
| R-12 | Advisory poll mistaken for legally valid vote | Medium/high | Explicit labels, separate external formal process; controller |
| R-13 | Later design interpreted as mandatory MVP1 platform build | High/high | Release-scoped migrations/endpoints and backlog review; delivery lead |
| R-14 | Part-time support mistaken for continuous staffed coverage | Medium/high | Named backup and contracted/rotating coverage before SLA promise; product/platform |
| R-15 | Joint ownership, tenancy and debt transfer rules differ locally | High/high | Written jurisdiction/accountant examples before finance; controller |

## Questions and validation deadlines

| ID | Question / proposed answer for design | Validate by / owner |
| --- | --- | --- |
| Q-01 | Is association the controller/contract boundary? Proposed yes; independent associations separate | Before tenancy/schema implementation; sponsor/controller |
| Q-02 | Which jurisdiction(s), data residency and processor/subprocessor terms apply? Unconfirmed | Before real personal data or hosting purchase; legal/privacy |
| Q-03 | Does pilot accept English, text-only issue flow and daily administrator response? Proposed yes | Before MVP1 UX acceptance; pilot sponsor |
| Q-04 | Actual unit/users/peak volume, budget and available team skills? Use A-02–05 until verified | Before delivery commitment/provider sizing; delivery lead |
| Q-05 | Managed OIDC/email availability, sender domain, MFA assurance/recovery, quotas and contracts? Not chosen | Initial vertical slice, before production integration; security/platform |
| Q-06 | Ownership/occupancy evidence, co-owner access, move-out and former-member history policy? Separate dated access recommended | Before resident onboarding; association/privacy |
| Q-07 | Required RPO/RTO, availability and staffed coverage? Proposed 15 min/4h/99.5% | Before production hosting/SLA; service owner |
| Q-08 | Record retention, legal holds, privacy response deadlines and breach notification rules? No legal default asserted | Before pilot, then before each sensitive feature; specialist/controller |
| Q-09 | Currency, liability party/joint liability, proration, rounding, accounting/control accounts and approval thresholds? Proposed formulas need examples | Before MVP3 implementation; accountant/finance owner |
| Q-10 | Existing opening balances and external accounting integration source? Assume manual reviewed cutover | Before finance scope/estimate commitment; finance owner |
| Q-11 | Meter units/replacement/rollover/shared residual and tariffs? Flat tariff only proposed | Before MVP4; utilities/finance owner |
| Q-12 | Facility capacity, cancellation and poll participation policy? Exclusive resources/advisory poll proposed | Before MVP5; association |

**Safe to defer:** exact attachment scanner/store until MVP2 planning; finance provider/ledger policies until MVP3 discovery (before coding finance); asset/meter catalogue until MVP4; poll/resource rules until MVP5; microservice/broker/cache choice until measured trigger; online payment/SaaS billing/formal-voting integration indefinitely outside baseline. Deferral does not remove MVP1 privacy/isolation/recovery decisions.
