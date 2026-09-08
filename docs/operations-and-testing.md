# Operations and testing

[Roadmap and release gates](mvp-roadmap.md) · [Use cases](use-case-catalog.md) · [NFRs](solution-design.md)

## 1. Test strategy and evidence

Each use case defines AT-UC-... acceptance IDs. Implement tests against the actual chosen stack and PostgreSQL, not only mocked repositories or an in-memory SQL substitute that cannot enforce RLS/locks. A proposed acceptance criterion is not a passed test. Record build/image digest, schema version, environment, fixture revision, test identity and result reference in release evidence.

| Level | Coverage / purpose | Cadence and owner |
| --- | --- | --- |
| Domain unit/property | State transitions, grant effective dates, last-admin rule, money/rounding/allocation, tariff/recurrence calculations | Each change; domain engineers |
| Database integration | Composite tenant FKs, FORCE RLS, connection pool reset, lock races, uniqueness, transaction rollback, migrations | Each data/security change; backend + QA |
| API/contract | Exact routes, response/status/ETag/idempotency, identity callback behavior, versioned job schemas/provider adapter | PR pipeline with deterministic provider stubs |
| Browser E2E | Resident onboarding-to-closure/reopen, admin replacement, tenant switching, current access denial, mobile keyboard/error flows | Critical smoke each release; extended suite nightly or relevant change |
| Security/authorization | Two tenants and denied resources within one tenant; session/CSRF/MFA, object-level checks, provider failure | PR targeted suite and full release gate |
| File/integration fault | Upload quarantine, content mismatch, scan outage, orphan cleanup, email accepted-then-crash/unknown status | MVP2+ or adapter change; targeted MVP1 email tests |
| Finance verification | Independently calculated journal examples, exact arithmetic and balanced posting properties, payment allocation races, closure serialization | Required before MVP3; targeted each finance change |
| Migration/recovery | Previous version upgrade with two tenants, interrupted migration, app rollback compatibility, isolated PITR restore and deletion replay | Each schema release; full restore before pilot/quarterly |
| Load/accessibility | Stated concurrency/latency and mobile critical paths, keyboard, screen reader, contrast and zoom | Baseline launch and material UI/data workload changes |

Coverage percentage alone is not a release gate. Critical branch/race tests and known business examples matter more than a target number of trivial tests. Unit-test math independently from implementation examples, including bounds, zero values, uneven allocation and reversals. Do not broaden testing without a changed risk; retain reproducible suites for release gates.

### Isolation and abuse matrix

Create tenants A and B with different organizations; A has buildings A1/A2 and units A101/A102; B has B1/B101. Users: resident RA in A101 only; co-occupant RC in A101; resident RB in B101; administrator AA scoped to A1 only; identity AB administrator in A and resident in B; suspended/removed users; operator with metadata permissions only. Include identical human labels and emails in tenant-local Person records to expose accidental global joins.

| Test ID | Attack/failure | Required outcome |
| --- | --- | --- |
| SEC-01 | Replace tenant path/header/body/context with B while A session selected | 409/403/404 as defined, no B records/counts/change |
| SEC-02 | RA guesses A102 unit, issue or meter ID | 404, no title/history/reading leaked |
| SEC-03 | RC guesses RA issue in same unit | 404 because reporter-only scope is additional to unit grant |
| SEC-04 | AA requests A2 admin/export/recipient list | Deny unless explicit A2 grant; same tenant RLS alone is insufficient |
| SEC-05 | AB switches A admin to B resident; old tab posts admin command | Context version blocks stale write; B resident role enforced |
| SEC-06 | Construct composite foreign key with A row and B parent | Database rejects; API normalizes error without exposing B |
| SEC-07 | Query with missing tenant setting; use pooled connection after A rollback then B request | Fail closed; B never inherits A context |
| SEC-08 | Execute app runtime queries against tables/views with owner/definer bypass paths | Runtime has no bypass path; inspect grants, policies and view/function security |
| SEC-09 | Copy attachment/document/export download path to RB or removed RA | No bytes/metadata; URLs require current parent permission |
| SEC-10 | Upload declares JPEG but bytes executable/PDF, path traversal name, oversized/decompression bomb | Reject/quarantine; no readable object; no client-chosen key |
| SEC-11 | Change parent attachment binding to foreign tenant/resource, reuse clean upload | Reject before visibility; no cross-parent publication |
| SEC-12 | Request report as admin then revoke before job starts or download | Suppressed generation or denied download; no emailed content |
| SEC-13 | Cache list/count under shared key and switch tenant in same browser | Clear/discard old state; query keys include tenant/context; no service-worker business cache |
| SEC-14 | Forged job tenant/resource pair or recipient in B | Worker rejects mismatched scope; no B read/send/write |
| SEC-15 | Queued notice/issue update after resident move-out | Suppressed, except minimal mandatory access-end metadata message |
| SEC-16 | Recipient batch uses CC or mixes A and B audiences | Separate recipient deliveries, no shared contact list; all candidates tenant-scoped |
| SEC-17 | Search/export/constraint error/log trace includes hidden resource detail | Safe projection/error and redacted logs; zero private payload |
| SEC-18 | Operator calls resident issue/finance/download route | Denied; bounded recovery command only with approved ticket |
| SEC-19 | CSRF missing, forged origin, expired/replayed session, failed admin MFA | Reject unsafe/privileged action; no bypass through API clients |
| SEC-20 | Two last-admin revocations, revocation racing protected write | Control-row lock preserves coverage; no write commits after required grant is revoked |
| SEC-21 | New owner reads former liability account | No access/debt inheritance without explicit reviewed account grant/transfer |
| SEC-22 | Privacy package for former resident includes other subjects/current building data | Reviewed minimization; only explicit response grant, no normal tenant reactivation |
| SEC-23 | Hold/deletion tombstone survives backup restore | Restored service stays isolated until hold/tombstone replay; erased data not re-exposed |
| SEC-24 | Mass-assignment adds steward/finance flags to resident invite payload | Reject unexpected privilege fields; dedicated admin transfer policy only |

### Concurrency and fault cases

FIN-T01: pool 10001 split across 3 equal accounts sums exactly and tie-break stable. FIN-T02: random valid journal operations preserve balanced totals and balance equals posted projections. FIN-T03: two allocators cannot exceed receipt/charge remainder. FIN-T04: post versus period close serializes. FIN-T05: duplicate receipt/charge/recurrence keys never double-post after timeout. FIN-T06: correction/reversal retains original posting and uses open period. FIN-T07: owner transfer preserves liability and privacy. FIN-T08: snapshot statements exclude drafts and freeze closed-period cutoff.

REL-T01: kill worker after lease claim; lease expires and safe retry occurs. REL-T02: kill after domain commit before email attempt; message remains eligible. REL-T03: provider accepts then worker loses connection before marking Accepted; classify Unknown and reconcile, acknowledge duplicate risk. REL-T04: outage for 24h leaves failed deliveries visible and domain records committed. FILE-T01: storage succeeds but metadata update fails; orphan quarantined/cleaned. FILE-T02: scanner response belongs to old object version; cannot mark replacement clean. RES-T01: overlap race has one winner; adjacent intervals both succeed. GOV-T01: poll close versus ballot has deterministic ordering. UTL-T01: tariff boundary, replacement, rollover and missing readings cannot silently fabricate billable usage.

Development uses synthetic tenants and an OIDC test realm/provider sandbox, email capture sink, object-store test bucket and deterministic scan-status stub; staging uses the selected real provider sandbox and malware test file approved for security testing. A stub is useful for failure cases but does not prove provider compatibility. No real resident data in development. Masked/subset migration fixtures require controller approval.

## 2. Environments and deployment

Development: per-developer app/PostgreSQL with synthetic tenants, deterministic time provider, email sink and documented fixtures. Staging: separate account/project, secrets, database, IdP client and sender/storage sandbox; mirrors runtime/IAM/network/migrations. Production: independently controlled identities/secrets and backups in approved region; no staging access to production buckets/tokens. Emergency recovery environment is isolated with outbound email/jobs disabled.

CI/CD sequence: review domain/ADR/API changes → formatting/static/security/dependency/SBOM checks → targeted unit and actual-PostgreSQL integration suites → build immutable image and migration artifact → deploy staging → run critical E2E, isolation, migration compatibility and relevant NFR checks → human release owner verifies G gates → restricted production migration/app deployment → authenticated smoke and alert verification. Required approvals are concrete release evidence, not a substitute for doing staging work. Documentation changes update traceability and Mermaid/link checks.

Configuration includes approved IdP issuer/client/redirects, session/crypto key references, database credentials, sender domain/provider, tenant-independent deployment region, rate caps, retention defaults and feature-release flags. Fail startup on missing/invalid security configuration. Secrets are never in source, browser bundle, export or logs. Use workload identity where supported and short-lived credentials; verify actual platform capability. Key rotation accepts old decryption keys for an approved overlap, encrypts new sessions with current key, and tests rollback compatibility.

Health endpoints: `/health/live` reports process liveness without dependency probes; `/health/ready` tests minimal database connectivity and schema compatibility, but does not fail because email is temporarily unavailable; `/health/worker` is operator-restricted freshness/lease/queue summary. No health endpoint exposes tenant counts, usernames or credentials. Synthetic monitor exercises a dedicated synthetic tenant issue read; test traffic is excluded from product metrics.

Deploy expand-only schema first, application second, backfill under rate limits, verify and later contract. Hold migrations with an exclusive deployment lock; do not run on every replica startup. Runtime is non-owner. Rollback application image only if schema compatibility matrix permits; otherwise feature disable plus forward corrective migration. Do not run destructive down-migrations automatically. A restore is last resort for corruption and includes a deliberate RPO/loss/reconciliation decision.

### Operational signals and alert thresholds

| Signal | Proposed alert | Owner / first response |
| --- | --- | --- |
| Synthetic availability / API errors | 3 consecutive failures or >5% 5xx for 5 min | On-call; inspect deployment/dependency status |
| Latency | p95 above NFR-02 for 15 min | Engineering/platform; query saturation and connection pool |
| Outbox age / failed deliveries | Oldest eligible >10 min or >5% permanent failures/hour | Platform; building admin verifies contacts |
| Worker heartbeat/lease | No heartbeat 2 min or growing expired lease backlog | Platform; restart healthy version, replay safely |
| Backup/PITR freshness | Recovery-point age >15 min or failed scheduled backup | Platform/database owner immediately |
| Capacity | Disk >75%, projected <14-day headroom, sustained CPU >80% 15 min | Platform; measured scale/query review |
| Security | Privileged MFA failure anomaly, repeated cross-scope attempts, operator emergency command | Security owner; investigate without raw payload logging |
| Finance | Any unbalanced projection/control mismatch, repeated posting failures | Disable affected posting route and alert finance owner |
| Files/reports | Scan queue >10 min, rejected object readable, export scope failure | Disable uploads/download affected version; security review |

Measure correlation IDs, tenant pseudonymous ID, action, latency, status, event/job IDs and safe counters. Do not use resident identity/tenant name as unbounded metrics labels. Restrict log access; sampled trace attributes contain no issue body, tokens, ballot choices, invoice narrative or file bytes. Audit access to audit/log tooling itself. Track business response-time metrics through authorized aggregates, not leaked diagnostic logs.

## 3. Recovery and pilot cutover

Automated encrypted backups plus continuous WAL/PITR support the proposed 15-minute RPO. PostgreSQL describes continuous archive recovery as base backup plus replayed WAL; the exact managed provider configuration and restore limits must be verified. See [PostgreSQL PITR documentation](https://www.postgresql.org/docs/current/continuous-archiving.html). Backup retention 35 days is a product proposal, not a legal conclusion. Retain configuration, encryption-key recovery access and deployment artifacts separately. A backup without usable keys and schema-compatible image is insufficient.

Full restore process: incident owner chooses restore point and maximum acceptable loss; restore database into isolated project with outbound jobs off; restore/mount matching object versions from MVP2; apply current approved deletion tombstones/holds and invalidate old sessions as needed; verify checksums, row/control totals, tenant isolation and sample issue/financial workflows; reconcile external email/payment records after chosen point; security/platform approve switching ingress; monitor. Record measured RTO/RPO and actual lost/replayed records. Database failover and backup restore are different procedures.

Single-tenant recovery in shared schema is harder: restore entire database into isolation, select target tenant via audited export/extraction including dependent rows/object versions, compare with current tenant, prepare merge plan or replace target tenant during explicit maintenance. Never overwrite all tenants to fix one tenant. Financial/identity references and post-restore changes require reconciliation. No self-service one-click per-tenant PITR promise in MVP1; support procedure must be rehearsed with synthetic A/B.

Pilot cutover: (1) validate controller/privacy/hosting decisions and name active admin plus backup; (2) record baseline request volumes and response practices; (3) verify 60-unit register and initial dated grants with second reviewer; (4) create tenant/steward and test invitation to two consenting pilot accounts; (5) rehearse full issue loop and recovery in staging; (6) send invitations in batches of 20 with monitored bounce handling; (7) train administrator to check queue at least daily and residents on emergency/private issue rules; (8) use one authoritative portal issue queue, record phone-reported issues on behalf when consent/authority permits; (9) review activation, response and failures after day 1, day 7 and week 4. Rollback pilot onboarding by pausing new invites, exporting current issues and handing them back to verified administrator; retain authorized records and history, do not erase evidence to hide failure.

MVP3 adds a finance cutover: freeze source as-of date, reconcile account liability and total opening to external books, import/approve openings in test and then production, parallel-run one billing period, compare all control totals, obtain accountant sign-off, mark source/target responsibility dates. No double receipt posting or silent corrections between ledgers.

## 4. Minimum operational runbooks

### OP-001 — Incident triage

**Trigger:** outage, suspected leak, integrity alert or critical dependency failure. **Owner:** on-call platform lead, security lead for possible exposure. **Steps:** acknowledge within 15 minutes during agreed coverage; classify SEV1 suspected cross-tenant leak/corruption/unavailable service, SEV2 major degradation, SEV3 individual workflow; freeze risky deployment/feature or suspend affected tenant with approved command; preserve redacted evidence; identify scope via request/event IDs; correct or restore under OP-002/005; test two-tenant denial plus affected positive flow; product/controller owner sends appropriate user and regulatory communications through approved channels. Do not expose residents in incident chat.

**Recovery criterion:** affected journey passes, integrity/isolation confirmed, metrics normal for 30 minutes. **Escalation:** security/controller decides notification obligations using jurisdiction policy; no invented universal reporting deadline. **Evidence:** timeline, scope, changes, decision owner, follow-up with assigned due dates. If sole operator unavailable, named backup must have tested access; no unsupported 24/7 SLA promise.

### OP-002 — Backup restore

**Trigger:** corruption, lost data or scheduled drill. **Owner:** platform/database owner; security/controller approves re-exposure. **Steps:** choose restore point; isolate destination and disable outbound jobs; restore database/keys/compatible image and object versions; apply erasure tombstones; validate counts, grants, issue samples and finance control totals; reconcile side effects after restore point; approve cutover; switch ingress and watch alarms. Record actual loss/RTO. For one tenant use isolated extraction/merge plan above, preserving B while recovering A.

**Stop/escalate:** missing key, uncertain source checkpoint, finance imbalance or isolation failure prevents cutover. **Success:** verified recoverable point <=15 minutes old and service <=4 hours under drill assumptions, with evidence. If missed, readiness gate fails until target is adjusted with business owner or design improved.

### OP-003 — Failed jobs/notifications/integrations

**Trigger:** queue-age/heartbeat or failed/Unknown outcome. **Owner:** platform operator; building administrator handles verified contact correction. **Steps:** inspect metadata only; classify transient/permanent/unknown; verify tenant/resource/recipient remains eligible; fix provider credentials/quota/network or scanner service; reconcile unknown provider reference; requeue original event/occurrence key through bounded command; verify one domain result and actual delivery state. Quarantine unsupported event version. Never modify tenant ID in a queued payload to make it work.

**Files:** pending scan remains unreadable; reconcile checksum/object version, clean or delete unbound 24-hour orphans. **Reports:** revoked requester means Suppressed; remove output instead of retrying disclosure. **Success:** backlog decreasing, terminal state recorded, no duplicate posting; any unavoidable duplicate email is accepted as a delivery limitation and recorded. **Escalate:** persistent >24h failure, suspected wrong recipient, lost side-effect reconciliation capability.

### OP-004 — Access and privileged recovery

**Trigger:** move-out/suspension/removal, admin replacement or lost last-admin access. **Owner:** tenant steward; recovery requires two authorized operators and verified organizational requester. **Steps:** normal actions use IAM-007/008 and dated BLD-002; check exact tenant/person/building/unit/date; commit revocation/grant change; verify old session/download denies and unrelated membership still works. For emergency recovery, independently verify authority through approved records/contact, create time-limited single-command SupportAuthorization, second approve, issue new steward invitation, candidate accepts with MFA, then retire old grants as appropriate. Audit and notify accountable contacts.

**Never:** unrestricted impersonation, shared password, disabling MFA without approved recovery policy, arbitrary SQL grant edits. **Success:** active coverage preserved and only authorized scope gained; review emergency access within one business day.

### OP-005 — Deployment and rollback

**Trigger:** approved release or urgent fix. **Owner:** release owner and platform engineer. **Steps:** verify all relevant G gates, immutable artifacts, backup freshness and schema compatibility; run restricted reviewed migration; deploy new image; smoke own-tenant flow and foreign-tenant denial; watch metrics; if failures, pause release and roll back compatible image or disable feature/roll forward. Restore only for corruption under OP-002. Keep previous image/config/key overlap available.

**Stop:** failed isolation/financial check, missing recovery point, destructive migration without approved plan. **Success:** critical journey and health normal for 30 minutes, version evidence recorded.

### OP-006 — Tenant onboarding and offboarding

**Trigger:** approved organization onboarding or verified exit request. **Owner:** onboarding operator + steward. **Onboard:** verify authority/controller contact, region/policy, initial administrator/MFA, create tenant and bounded invite, validate unit register, staged invitations and pilot checks. **Exit:** verify steward request; create authenticated snapshot export and receipt/waiver; suspend normal activity; cancel/pause jobs; record retention and holds; archive; at eligible date obtain second purge approval; delete scoped rows/objects through retryable process; preserve pseudonymous audit/tombstones and other-tenant identity memberships; verify aggregate deletion and backup expiry schedule.

**Stop:** legal hold, incomplete export/waiver, ambiguity in organization authority or cross-tenant object reference. **Success:** documented handover and denied ordinary access, retained obligations tracked, eventual purge evidence recorded.

### OP-007 — Privacy and retention

**Trigger:** personal-data request, correction, retention expiry or legal hold. **Owner:** controller-designated privacy representative. **MVP1:** use restricted ticket register and approved bounded commands; MVP2 portal automates tracking. Verify subject identity proportionately; classify request/jurisdiction deadline; find subject data in requested tenant; review third-party redactions and legal holds; decide fulfill/partial/defer with basis; deliver reviewed package via authenticated subject-only channel or verified secure handover; append corrections/pseudonymize eligible data; retain mandated history and deletion tombstone; record completion and reviewer.

**Stop:** uncertain identity, unsupported authority, unreviewed third-party information. **Success:** justified response within locally approved deadline, no restored building access, response artifact expires. No automatic assertion that deletion request overrides statutory retention.

### OP-008 — Finance discrepancies

**Trigger:** control-total mismatch, duplicate source, wrong allocation or questioned statement. **Owner:** finance administrator + independent accountant/reviewer. Freeze affected posting batch/period closure, retain evidence and original journal, compare source statement and immutable posting/source keys, identify allocation versus cash-posting error, produce FIN-007/008 correction proposal and required approval, post current-period compensation, rebuild/reconcile projection and reissue versioned statement if required. Never edit amount column of Posted record or reopen period via SQL.

**Success:** journal and external control totals reconcile or explicitly documented approved suspense remains; affected debtor gets private corrected-statement link. **Escalate:** unexplained money movement, fraud suspicion, inability to reproduce allocation or incorrect liability basis.

## 5. Steady-state operations staffing

For the first-year scale, propose platform/SRE 0.1–0.2 FTE, application maintainer 0.1–0.2 FTE, support/onboarding 0.1–0.25 FTE and security/privacy coordination 0.05–0.1 FTE, plus association administrator/accountant effort owned by each tenant. These are average workload allocations, not a 24/7 staffed rota. Contract external/rotating emergency coverage if promised availability requires it; at least two trained people must be able to restore and recover admin access. MVP3 requires a named finance escalation owner; MVP4 adds meter/maintenance policy owner. Measure real incident/onboarding volume monthly and adjust staffing.
