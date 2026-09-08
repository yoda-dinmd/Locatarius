# Complete use-case catalogue

[Architecture](solution-design.md) · [Roadmap](mvp-roadmap.md) · [Traceability](traceability.md)

The baseline contains **55 business use cases**. Each ID links to a dedicated full specification. Relative effort is an ordinal comparison, not additive story points: S = one simple flow/policy; M = a multi-step aggregate flow; L = several actors/states or significant isolation, concurrency, financial or integration risk. MVP1 use cases are Must; later cases are Must within their assigned increment, while the decision to fund that increment remains conditional. Dependencies identify required capabilities, not a requirement to exercise every predecessor in every user session.

Business requirements BR-01–12 and optional/excluded decisions are authoritative in [solution-design.md](solution-design.md). Technical enablers EN and operational procedures OP below are not additional resident business use cases.

## MVP1 — 19 use cases

| ID / title | Module; actor | Outcome | Requirement | Priority / effort | Dependencies | Major risk |
| --- | --- | --- | --- | --- | --- | --- |
| [UC-IAM-001 — Sign in or recover an account](use-cases/UC-IAM-001.md) | Identity; User | access the correct memberships without the platform storing passwords | BR-01 | Must / L | Foundation enablers | Cancelled sign-in returns to sign-in |
| [UC-IAM-002 — Sign out and end application access](use-cases/UC-IAM-002.md) | Identity; User | end the current application session on a shared device | BR-01 | Must / M | UC-IAM-001 | Expired session still returns signed-out result |
| [UC-IAM-003 — Accept an invitation](use-cases/UC-IAM-003.md) | Identity; Invited user | activate the precise membership and access grants intended by the administrator | BR-01 BR-03 | Must / M | UC-IAM-001 UC-IAM-006 | Wrong identity, expired/revoked invitation or suspended tenant denies activation |
| [UC-IAM-004 — Switch active tenant safely](use-cases/UC-IAM-004.md) | Identity; User | work in one organization at a time without mixing data from other memberships | BR-02 | Must / M | UC-IAM-001 UC-IAM-003 | Suspended membership rejects switch |
| [UC-IAM-006 — Invite residents and manage pending invitations](use-cases/UC-IAM-006.md) | Identity; Building Administrator | give verified residents limited access to selected units | BR-01 BR-03 | Must / M | UC-TEN-001 UC-BLD-001 | Duplicate active invitation returns existing invitation summary |
| [UC-IAM-007 — Suspend, reinstate or remove a membership](use-cases/UC-IAM-007.md) | Identity; Building Administrator | end inappropriate access while retaining historical accountability | BR-02 BR-03 | Must / M | UC-IAM-008 | Last-administrator conflict returns 409 with replacement action |
| [UC-IAM-008 — Assign or replace administrators](use-cases/UC-IAM-008.md) | Identity; Tenant steward | maintain accountable administration without orphaning the organization | BR-02 BR-03 | Must / L | UC-TEN-001 | Unaccepted candidate leaves predecessor active |
| [UC-TEN-001 — Provision a tenant and first administrator](use-cases/UC-TEN-001.md) | Tenant lifecycle; Platform Operator | onboard an authorized organization with an accountable initial administrator | BR-02 BR-11 | Must / L | Foundation enablers | Duplicate onboarding reference returns existing tenant |
| [UC-TEN-002 — Maintain settings and control tenant suspension](use-cases/UC-TEN-002.md) | Tenant lifecycle; Tenant steward | keep operational settings accurate and stop tenant access when necessary | BR-02 BR-11 | Must / M | UC-TEN-001 | Unauthorized state change returns 403 |
| [UC-TEN-003 — Export an organization for handover](use-cases/UC-TEN-003.md) | Tenant lifecycle; Tenant steward | obtain a portable record set for administrator handover or exit | BR-11 BR-12 | Must / M | UC-TEN-001 | Mid-stream failure discards incomplete client file; export attempt audit remains |
| [UC-TEN-004 — Archive and offboard a tenant](use-cases/UC-TEN-004.md) | Tenant lifecycle; Platform Operator | end service predictably while preserving approved retention obligations | BR-11 BR-12 | Must / M | UC-TEN-002 UC-TEN-003 | Hold or missing approval blocks purge |
| [UC-BLD-001 — Establish and maintain the building register](use-cases/UC-BLD-001.md) | Building register; Building Administrator | identify the buildings and units used to route residents and requests | BR-03 | Must / M | UC-TEN-001 | Duplicate labels return 409 |
| [UC-BLD-002 — Record ownership, move-in, move-out and access dates](use-cases/UC-BLD-002.md) | Building register; Building Administrator | maintain truthful unit history and end access at the correct time | BR-03 BR-02 | Must / L | UC-BLD-001 UC-IAM-008 | Backdated correction requires reason and audit, cannot erase prior activity or silently rewrite MVP3 liability |
| [UC-ISS-001 — Report a building issue](use-cases/UC-ISS-001.md) | Issues; Resident | create a tracked maintenance request or complaint with a visible reference | BR-05 | Must / M | UC-IAM-003 UC-IAM-004 UC-BLD-002 | Invalid location or missing text returns 422 |
| [UC-ISS-002 — Follow an issue and exchange comments](use-cases/UC-ISS-002.md) | Issues; Resident or Building Administrator | keep the reporter and responsible administrators in one traceable conversation | BR-05 | Must / M | UC-ISS-001 | Concurrent comment appends are allowed with unique client keys; state changes that disallow commenting cause 409 |
| [UC-ISS-003 — Triage and assign an issue](use-cases/UC-ISS-003.md) | Issues; Building Administrator | give each request a accountable owner and visible progress state | BR-05 | Must / M | UC-ISS-001 | Stale ETag returns 412 without overwrite |
| [UC-ISS-004 — Resolve an issue and confirm closure](use-cases/UC-ISS-004.md) | Issues; Building Administrator | communicate a concrete resolution and close the request with accountability | BR-05 | Must / M | UC-ISS-002 UC-ISS-003 | Resolution from stale state returns 412/409 |
| [UC-ISS-005 — Reopen an unresolved problem](use-cases/UC-ISS-005.md) | Issues; Resident or Building Administrator | restore follow-up when a recorded resolution did not solve the problem | BR-05 | Must / M | UC-ISS-004 | Already open returns 409 unless same request replay |
| [UC-RPT-001 — Review a role-scoped operational dashboard](use-cases/UC-RPT-001.md) | Reporting; Resident or Building Administrator | know which issue needs action and whether reported problems are progressing | BR-10 BR-05 | Must / M | UC-ISS-001 | Unsupported filter returns 422 |

## MVP2 — 10 use cases

| ID / title | Module; actor | Outcome | Requirement | Priority / effort | Dependencies | Major risk |
| --- | --- | --- | --- | --- | --- | --- |
| [UC-IAM-005 — Update profile and notification preferences](use-cases/UC-IAM-005.md) | Identity; User | keep contact details and optional notification settings current | BR-01 BR-04 | Must / M | UC-IAM-004 | Invalid locale/phone yields field errors |
| [UC-BLD-003 — Preview and commit a register import](use-cases/UC-BLD-003.md) | Building register; Building Administrator | migrate a controlled resident register without bulk access mistakes | BR-03 BR-12 | Must / L | UC-BLD-002 UC-ISS-006 | Stale preview or changed source requires new preview |
| [UC-COM-001 — Publish, amend or withdraw a targeted announcement](use-cases/UC-COM-001.md) | Communication; Building Administrator | inform the intended residents with an authoritative, versioned notice | BR-04 | Must / M | UC-ISS-006 UC-IAM-005 | Invalid audience rejects all targets |
| [UC-COM-002 — Read and acknowledge announcements](use-cases/UC-COM-002.md) | Communication; Resident | find current notices and acknowledge explicitly requested reading | BR-04 | Must / M | UC-COM-001 | Expired/withdrawn notice returns 404 with neutral unavailable message |
| [UC-COM-003 — Publish and retire building documents](use-cases/UC-COM-003.md) | Communication; Building Administrator | maintain a reliable document library with controlled versions and audiences | BR-04 | Must / M | UC-ISS-006 UC-COM-001 | Pending/rejected attachment returns 409 |
| [UC-COM-004 — Find and download authorized documents](use-cases/UC-COM-004.md) | Communication; Resident | retrieve a current document without exposing private object URLs | BR-04 | Must / M | UC-COM-003 | Stale/retired/missing object returns neutral unavailable state; no bucket paths exposed |
| [UC-COM-005 — Review notification delivery and retry failures](use-cases/UC-COM-005.md) | Communication; Building Administrator | detect undelivered communication and correct the contact workflow | BR-04 BR-11 | Must / M | UC-COM-001 | Unknown provider outcome enters reconciliation before retry |
| [UC-ISS-006 — Add and access issue evidence](use-cases/UC-ISS-006.md) | Issues; Resident or Building Administrator | provide photos or documents that clarify an authorized issue | BR-05 BR-04 | Must / L | UC-ISS-002 | Exceeded quota/type rejects before storage |
| [UC-PRV-001 — Request personal data access, correction or deletion review](use-cases/UC-PRV-001.md) | Privacy; User | ask the responsible organization to address personal-data rights or errors | BR-12 | Must / M | UC-IAM-001 UC-TEN-004 | Unrelated tenant request returns neutral support route |
| [UC-PRV-002 — Review and fulfill a privacy case](use-cases/UC-PRV-002.md) | Privacy; Tenant steward | provide justified data access or correction while protecting third parties and retained records | BR-12 | Must / L | UC-PRV-001 UC-ISS-006 | Unverified requester or third-party data blocks release |

## MVP3 — 12 use cases

| ID / title | Module; actor | Outcome | Requirement | Priority / effort | Dependencies | Major risk |
| --- | --- | --- | --- | --- | --- | --- |
| [UC-FIN-001 — Establish debtor accounts and opening balances](use-cases/UC-FIN-001.md) | Finance; Building Administrator | start financial transparency from reconciled liabilities rather than ambiguous unit totals | BR-07 | Must / L | UC-BLD-002 | Unreconciled source blocks posting |
| [UC-FIN-002 — Define charge rules and billing periods](use-cases/UC-FIN-002.md) | Finance; Building Administrator | approve transparent dues and allocation rules before charging residents | BR-07 | Must / L | UC-FIN-001 | Overlap/conflicting rule version or missing weight prevents activation |
| [UC-FIN-003 — Preview and post a charge batch](use-cases/UC-FIN-003.md) | Finance; Building Administrator | issue reproducible charges with no partial billing | BR-07 | Must / L | UC-FIN-002 | Stale preview returns 409 and new preview required |
| [UC-FIN-004 — View account activity and statements](use-cases/UC-FIN-004.md) | Finance; Resident | understand exactly what is owed, paid and still open | BR-07 | Must / M | UC-FIN-003 UC-FIN-005 | Unknown/unauthorized account returns 404 |
| [UC-FIN-005 — Record an externally received payment](use-cases/UC-FIN-005.md) | Finance; Building Administrator | recognize money already received through bank or cash channels | BR-07 | Must / L | UC-FIN-001 UC-FIN-002 | Duplicate reference returns existing payment if identical, conflict if amount differs |
| [UC-FIN-006 — Allocate payments and match suspense receipts](use-cases/UC-FIN-006.md) | Finance; Building Administrator | apply paid funds to the correct debtor and outstanding charges | BR-07 | Must / L | UC-FIN-003 UC-FIN-005 | Concurrent allocator gets 409/412 and recomputes |
| [UC-FIN-007 — Issue a credit or reasoned balance adjustment](use-cases/UC-FIN-007.md) | Finance; Building Administrator | correct liability through visible financial entries | BR-07 | Must / L | UC-FIN-003 UC-FIN-006 | Missing reason/approval blocks posting |
| [UC-FIN-008 — Reverse erroneous postings or record external refunds](use-cases/UC-FIN-008.md) | Finance; Building Administrator | correct errors without erasing the accounting trail | BR-07 | Must / L | UC-FIN-006 UC-FIN-007 | Insufficient reversible amount returns 409 |
| [UC-FIN-009 — Reconcile receipts and close a billing period](use-cases/UC-FIN-009.md) | Finance; Building Administrator | confirm recorded money against external evidence and freeze an agreed period | BR-07 | Must / L | UC-FIN-008 UC-ISS-006 | Posting race waits on period lock then either commits before closure or is rejected |
| [UC-FIN-010 — Review arrears and send private reminders](use-cases/UC-FIN-010.md) | Finance; Building Administrator | follow up overdue balances without exposing debt to neighbors | BR-07 BR-04 | Must / M | UC-FIN-006 UC-COM-005 | Stale balances at dispatch trigger recalculation; paid accounts are suppressed |
| [UC-FIN-011 — Generate recurring charge drafts](use-cases/UC-FIN-011.md) | Finance; Scheduler | prepare each due billing batch once for administrator review | BR-07 | Must / M | UC-FIN-002 UC-FIN-003 | Duplicate delivery returns existing draft |
| [UC-RPT-002 — Generate scoped management reports and exports](use-cases/UC-RPT-002.md) | Reporting; Building Administrator | produce auditable operational and financial reports for authorized review | BR-10 BR-12 | Must / L | UC-TEN-003 UC-FIN-009 UC-ISS-006 | Revocation before execution suppresses report; revocation after generation denies download and schedules deletion |

## MVP4 — 9 use cases

| ID / title | Module; actor | Outcome | Requirement | Priority / effort | Dependencies | Major risk |
| --- | --- | --- | --- | --- | --- | --- |
| [UC-MNT-001 — Commission and complete a work order](use-cases/UC-MNT-001.md) | Maintenance; Building Administrator | coordinate actual repair work and connect completion to a resident issue | BR-06 | Must / L | UC-ISS-006 | Stale change rejected |
| [UC-MNT-002 — Maintain shared assets and locations](use-cases/UC-MNT-002.md) | Maintenance; Building Administrator | keep service history tied to a known common asset | BR-06 | Must / M | UC-BLD-001 | Duplicate asset tag returns 409 |
| [UC-MNT-003 — Schedule preventive maintenance and generate due work](use-cases/UC-MNT-003.md) | Maintenance; Building Administrator | avoid missing recurring maintenance without generating duplicate work | BR-06 | Must / L | UC-MNT-001 UC-MNT-002 | Worker retry cannot duplicate occurrence |
| [UC-MNT-004 — Record and correct maintenance expenses](use-cases/UC-MNT-004.md) | Maintenance; Building Administrator | record actual repair cost and provide a controlled handoff to finance | BR-06 BR-07 | Must / L | UC-MNT-001 UC-FIN-008 | Duplicate invoice reference is flagged for review; not automatically rejected across different vendors |
| [UC-UTL-001 — Register meters and effective associations](use-cases/UC-UTL-001.md) | Utilities; Building Administrator | attribute readings to the correct unit or common supply over time | BR-08 | Must / M | UC-BLD-002 UC-MNT-002 | Overlap returns 409 |
| [UC-UTL-002 — Submit a meter reading](use-cases/UC-UTL-002.md) | Utilities; Resident | report consumption evidence for an eligible meter and period | BR-08 | Must / M | UC-UTL-001 UC-ISS-006 | Duplicate meter/window/value returns previous result; conflicting second reading requires correction proposal |
| [UC-UTL-003 — Validate or correct meter readings](use-cases/UC-UTL-003.md) | Utilities; Building Administrator | approve reliable readings and preserve corrections after use | BR-08 | Must / L | UC-UTL-002 UC-FIN-008 | Concurrent approval returns 412 |
| [UC-UTL-004 — Publish tariffs and shared allocation rules](use-cases/UC-UTL-004.md) | Utilities; Building Administrator | establish explainable utility pricing before consumption is charged | BR-08 | Must / L | UC-UTL-001 UC-FIN-002 | Incompatible measurement units/currency rejected |
| [UC-UTL-005 — Calculate consumption and hand off charges](use-cases/UC-UTL-005.md) | Utilities; Building Administrator | convert accepted measurements into reviewed, traceable billing input | BR-08 BR-07 | Must / L | UC-UTL-003 UC-UTL-004 UC-FIN-003 | Stale source blocks commit and requires recalculation |

## MVP5 — 5 use cases

| ID / title | Module; actor | Outcome | Requirement | Priority / effort | Dependencies | Major risk |
| --- | --- | --- | --- | --- | --- | --- |
| [UC-GOV-001 — Organize a meeting and publish minutes](use-cases/UC-GOV-001.md) | Community; Building Administrator | give residents one place for meeting agenda, location and approved minutes | BR-09 | Must / M | UC-COM-003 UC-COM-005 | Invalid time zone or DST ambiguity requires explicit offset |
| [UC-GOV-002 — Create and close an informal poll](use-cases/UC-GOV-002.md) | Community; Building Administrator | collect advisory community preferences with transparent participation rules | BR-09 | Must / M | UC-COM-001 | Question edit after opening rejected |
| [UC-GOV-003 — Cast an advisory poll ballot](use-cases/UC-GOV-003.md) | Community; Resident | express a community preference once under the stated eligibility rule | BR-09 | Must / M | UC-GOV-002 | Duplicate same-key vote returns receipt; different choice after vote returns 409 |
| [UC-RES-001 — Define shared-resource availability](use-cases/UC-RES-001.md) | Reservations; Building Administrator | publish fair booking windows for common facilities | BR-09 | Must / M | UC-MNT-002 | Overlapping block and live reservation returns 409 plus scoped conflict list for administrator |
| [UC-RES-002 — Reserve or cancel a shared facility](use-cases/UC-RES-002.md) | Reservations; Resident | book an available resource without competing reservations for the same time | BR-09 | Must / L | UC-RES-001 | Concurrent conflicting booking returns 409 and refreshed availability |

## Technical enablers

| ID | Capability and release | Required evidence |
| --- | --- | --- |
| EN-001 | MVP1 reproducible repository/build, environments and migrations | Fresh install, staging deploy and rollback evidence |
| EN-002 | MVP1 tenant keys, RLS, authorization policies and negative fixtures | NFR-04/05 multi-tenant and cross-unit suite |
| EN-003 | MVP1 managed OIDC integration, sessions, CSRF and MFA | NFR-09 identity/session/recovery tests |
| EN-004 | MVP1 transactional outbox, email adapter, scheduler and fair retries | NFR-15 provider-failure and duplicate-send recovery |
| EN-005 | MVP1 audit, logging, metrics, health and alert routing | NFR-14/19 redaction and alert tests |
| EN-006 | MVP1 backup/PITR, restore, secrets and deployment safety | NFR-07/08/20 timed restore and release rehearsal |
| EN-007 | MVP1 accessibility, capacity, security and privacy readiness | NFR-01–03/10–13/17/19 critical journey validation |
| EN-008 | MVP2 private files, malware scanning and bounded source import | NFR-16 byte/URL/isolation/cleanup tests |
| EN-009 | MVP3 immutable financial posting and exact calculations | NFR-06 ledger, rounding, allocation and closure properties |
| EN-010 | MVP3 scoped asynchronous report generation | NFR-18 worker/download revocation and retry evidence |
| EN-011 | MVP4 effective utility evidence and recurrence scheduler | DST/reading correction/recurrence deduplication tests |
| EN-012 | MVP5 poll closure and reservation concurrency controls | Eligibility, ballot race and exclusive-booking tests |

## Operational procedure register

| ID | Procedure; first applicability | Owner |
| --- | --- | --- |
| OP-001 | Incident triage and communication; MVP1 | Platform on-call |
| OP-002 | Backup restoration and tenant recovery; MVP1 | Platform engineer + database owner |
| OP-003 | Failed jobs, notifications and integrations; MVP1 | Platform on-call; building admin for recipient correction |
| OP-004 | Access changes, privileged recovery and last-administrator emergency; MVP1 | Tenant steward + two authorized operators for recovery |
| OP-005 | Release deployment and rollback/roll-forward; MVP1 | Release owner + platform engineer |
| OP-006 | Tenant onboarding, export and offboarding; MVP1 | Onboarding operator + tenant steward |
| OP-007 | Privacy/retention request handling and legal holds; MVP1 manual, MVP2 portal | Controller/privacy representative |
| OP-008 | Financial discrepancy and period-close escalation; MVP3 | Finance administrator + accountant |

Detailed procedures and verification are in [operations-and-testing.md](operations-and-testing.md).
