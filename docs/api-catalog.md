# API catalogue and representative contracts

[Use cases](use-case-catalog.md) · [Data](data-model.md) · [Shared patterns](shared-patterns.md)

## API conventions

All paths are proposed public application contracts, not runnable implementation code. Base `/api/v1` is same-origin. Browser business operations use the server session; unsafe requests require CSRF and context version. Ordinary `/t/{t}` uses AUTH-01. `/platform` uses AUTH-02. Invitation acceptance and own privacy routes use AUTH-03/04. Auth redirects/callbacks follow their OIDC protocol exceptions. No browser direct database or object-storage credential. No arbitrary generic entity PATCH endpoint.

Lists use opaque keyset `cursor`, `limit` default 25/max 100, stable order `(createdAt,id)` or specified business date/id; respond `items,nextCursor,asOf`. Allowlisted filters include scoped building/unit, status and inclusive-start/exclusive-end time interval; never client SQL expressions. Lists and counts use the same authorization predicate. `GET` item includes ETag. State updates require If-Match; absent 428, stale 412. Create/append/post request keys follow CON-01. Async requests return 202 with a same-origin status URL and terminal states; a Ready file still requires authenticated download.

Common success: 200 read/update/action, 201 creation with Location, 202 queued job; protocols use 302 redirects. Common errors: ERR-01, plus provider unavailability 503 and rate limit 429. Generic safe problem contract is authoritative in shared-patterns.md; no per-endpoint different security error format. IDs are UUID-like opaque strings; timestamps ISO-8601 UTC, local legal dates YYYY-MM-DD, exact decimals/money transported as strings. Unknown enum or fields rejected where mass-assignment risk exists. APIs use additive minor changes; breaking semantic/schema changes require `/v2` and migration policy. Internal event/job major version is independent from HTTP version.

## Critical representative contracts

| Contract | Required request fields / controls | Response and domain validation |
| --- | --- | --- |
| POST invitations | email, personId, buildingIds, unitGrantPlans with validFrom/validTo; Idempotency-Key | 201 invitationId, Pending, expiresAt, deliveryState; reject any foreign-scope row atomically; never return raw stored token |
| POST invitations/accept | token; authenticated verified identity; CSRF | membershipId, tenantId, effective grants, tenantState; consume token once; current active tenant is irrelevant |
| POST session/tenant | tenantId, observedContextVersion; CSRF | tenantId, contextVersion, role/scopes; 409 on stale tab; no user-chosen role |
| POST issues | buildingId, optional unitId, category, title, description; request key/context | 201 issueId, reference, Open, version, createdAt, notificationState=Queued; 404 denied location, 422 text/closed unit |
| POST issues/{id}/resolve | resolutionSummary; If-Match; CSRF | 200 Resolved, resolvedAt, ETag; 412 stale, 409 disallowed source state; persisted audit/outbox |
| POST attachments then PUT content | parentType, parentId, length, mediaType, checksum; bounded bytes at server-issued ID | 201 upload reference then 202 PendingScan; only Clean state grants parent-authorized content; 413/422/503 explicit |
| POST charge-batches/preview then /{id}/post | periodId, source/rule versions; post previewDigest, expected version and durable key | preview includes per-account minor-unit amount, basis, rounding, totals; post returns journal IDs and Posted; stale basis 409 |
| POST payments | accountId optional, amountMinor as string, currency, channel, receivedAt, sourceReference, evidenceRef; durable key | 201 paymentId, postingId, unappliedMinor/suspenseMinor; source uniqueness protects retries; recording only |
| POST payments/{id}/allocations | allocations of chargeId/amountMinor; account/payment versions | committed allocation IDs and remainder; 409 insufficient/unavailable amount; all-or-nothing |
| POST reservations | resourceId, startAt, endAt; key and context | 201 Confirmed; 409 overlap/quota; original request retry returns same booking |
| POST reports | reportType, buildingIds, date range, CSV/Markdown, request key | 202 reportId,statusUrl; poll yields Pending/Ready/Failed/Suppressed; output TTL and download endpoint |

### Shared job and event payload mapping

| Family | Minimal payload beyond REL-01 envelope | Revalidation and durable deduplication |
| --- | --- | --- |
| invitation / membership / tenant | invitationId or membershipId, action, affected grant IDs, safe state | Invitation hash-bound scope or approved operator command; live expiry; protected grants |
| issue / announcement / document / meeting | resourceId, version, audience plan reference | Current resource/audience membership before dispatch; per-recipient delivery key |
| attachment.scan_requested.v1 | attachmentId, immutable object version, checksum | Parent scope and tenant; exact version scan result; never client key |
| finance.* | posting/batch/payment/account reference, occurrence if applicable | Account/period scope and current recipient; source/occurrence uniqueness persists |
| maintenance.generate-due.v1 / finance.generate-drafts.v1 | rule/schedule version ID, occurrence local date | Active tenant and source; unique tenant/source/occurrence |
| report.requested.v1 / privacy.case_decided.v1 | job/case ID, requester/subject, approved immutable scope reference | Reauthorize execution and download; deterministic output; privacy-specific response grant |
| poll.ballot_cast.v1 / reservation.state_changed.v1 | poll/ballot record ID or reservation ID; safe state only | No ballot choice in event; unique ballot and exclusion-protected reservation |

Each use-case event listed below has an internal schema registered before implementation. No webhook endpoint or provider payment operation is assumed in this baseline. Development stubs must reproduce provider accepted/failed/unknown outcomes rather than pretending every call succeeds.

## API-IAM-001

**[UC-IAM-001: Sign in or recover an account](use-cases/UC-IAM-001.md)** · MVP1 · OIDC / own session

**Purpose:** access the correct memberships without the platform storing passwords. **Scope:** Anonymous entry; after identity verification only the current user global profile and own membership directory are readable. Administrator routes require recent MFA.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /auth/login` | OIDC redirect/callback protocol; fixed destinations. |
| `GET /auth/callback` | OIDC redirect/callback protocol; fixed destinations. |
| `GET /api/v1/me/tenants` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /auth/recovery` | OIDC redirect/callback protocol; fixed destinations. |

**Key fields:** Return path from an allowlist; authorization code, state, nonce and PKCE verifier in the server callback.

**Result:** Authenticated session and membership selector, or onboarding-pending page if no active memberships. **Entities:** UserIdentity, Session, Membership.

**Errors/business conflicts:** Cancelled sign-in returns to sign-in. Invalid state, nonce, signature, issuer or audience rejects callback without session. Provider outage shows retry; existing valid sessions follow their normal lifetime. Recovery reveals no account-existence result in the platform.

**Concurrency/idempotency:** Protocol transaction/own-session rules in the use case; no ordinary command ETag. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-IAM-002

**[UC-IAM-002: Sign out and end application access](use-cases/UC-IAM-002.md)** · MVP1 · OIDC / own session

**Purpose:** end the current application session on a shared device. **Scope:** Current session only; no tenant selection required. Logout cannot revoke another user session.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /auth/logout` | OIDC redirect/callback protocol; fixed destinations. |
| `GET /auth/logout/callback` | OIDC redirect/callback protocol; fixed destinations. |

**Key fields:** CSRF token and optional allowlisted post-logout destination.

**Result:** Signed-out page; application session remains invalid even if provider logout fails. **Entities:** Session.

**Errors/business conflicts:** Expired session still returns signed-out result. Missing CSRF rejects POST. Provider timeout displays local sign-out success and provider-session uncertainty.

**Concurrency/idempotency:** Protocol transaction/own-session rules in the use case; no ordinary command ETag. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-IAM-003

**[UC-IAM-003: Accept an invitation](use-cases/UC-IAM-003.md)** · MVP1 · AUTH-03 invite

**Purpose:** activate the precise membership and access grants intended by the administrator. **Scope:** Authenticated identity with verified email matching the invitation; token grants access only to its invitation record, not to tenant data before acceptance.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/invitations/accept` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** One-use opaque invitation token; signed-in identity; explicit acceptance.

**Result:** Accepted invitation and membership; frontend shows accessible buildings or effective-start date. **Entities:** Invitation, Membership, BuildingGrant, UnitAccessGrant.

**Errors/business conflicts:** Wrong identity, expired/revoked invitation or suspended tenant denies activation. Concurrent acceptance has one winner; same identity replay returns existing accepted result. Removed memberships require a newly authorized invitation.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `membership.activated.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-IAM-004

**[UC-IAM-004: Switch active tenant safely](use-cases/UC-IAM-004.md)** · MVP1 · AUTH-01 session

**Purpose:** work in one organization at a time without mixing data from other memberships. **Scope:** Current identity and active membership in the requested tenant; role is resolved for that tenant on every request.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/session/tenant` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Target tenant ID and observed context version.

**Result:** New context version and role-scoped landing page; previous tenant lists are discarded. **Entities:** Session, Membership, Tenant.

**Errors/business conflicts:** Suspended membership rejects switch. Concurrent tabs receive 409 CONTEXT_CHANGED, must reload and explicitly reselect. Client-supplied roles are ignored.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-IAM-005

**[UC-IAM-005: Update profile and notification preferences](use-cases/UC-IAM-005.md)** · MVP2 · AUTH-01 session

**Purpose:** keep contact details and optional notification settings current. **Scope:** Own global profile; tenant preferences for own membership only. Administrators cannot change another identity login address.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/me` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `PATCH /api/v1/me` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/preferences` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `PUT /api/v1/t/{t}/preferences` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Display name up to 120 characters, locale, optional contact phone, tenant notification preferences and ETag.

**Result:** Updated profile and effective preferences with new version. **Entities:** UserIdentity, MembershipPreference.

**Errors/business conflicts:** Invalid locale/phone yields field errors. Unverified email remains in IdP pending state. Stale update returns 412; global changes do not copy preferences to other tenants.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-IAM-006

**[UC-IAM-006: Invite residents and manage pending invitations](use-cases/UC-IAM-006.md)** · MVP1 · AUTH-01 session

**Purpose:** give verified residents limited access to selected units. **Scope:** Administrator in every selected building; invitation role fixed to Resident. Administrator grants use UC-IAM-008. Only masked invitation addresses appear in broad lists.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/invitations` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/invitations` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/invitations/{id}/resend` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/invitations/{id}/revoke` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Email, person reference, selected buildings/units, access interval; resend or revoke command with invitation version.

**Result:** Pending invitation with delivery state and expiry; revoke makes any outstanding link unusable. **Entities:** Invitation, Person, Unit, OutboxMessage, Delivery.

**Errors/business conflicts:** Duplicate active invitation returns existing invitation summary. Out-of-scope unit returns 404. Email outage leaves pending invitation with failed-delivery indicator; authorized resend uses a fresh token.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `invitation.requested.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-IAM-007

**[UC-IAM-007: Suspend, reinstate or remove a membership](use-cases/UC-IAM-007.md)** · MVP1 · AUTH-01 session

**Purpose:** end inappropriate access while retaining historical accountability. **Scope:** Tenant steward may change tenant memberships; building administrator can end only unit/building grants they manage. No resident can revoke another user.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/memberships/{id}/state` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/access-grants/{id}/end` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Target membership or grant, action, effective timestamp, reason and ETag.

**Result:** Access denied on subsequent protected requests; other-tenant memberships remain active. **Entities:** Membership, BuildingGrant, UnitAccessGrant, AuditRecord.

**Errors/business conflicts:** Last-administrator conflict returns 409 with replacement action. Stale version returns 412. Future end time is evaluated per request even if scheduler is down.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `membership.access_changed.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-IAM-008

**[UC-IAM-008: Assign or replace administrators](use-cases/UC-IAM-008.md)** · MVP1 · AUTH-01 session

**Purpose:** maintain accountable administration without orphaning the organization. **Scope:** Building Administrator with tenant-steward permission and MFA within 15 minutes; Platform Operator emergency recovery follows OP-004 and does not browse tenant records.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/administrator-transfers` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/administrator-transfers/{id}/complete` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Candidate verified identity or invitation email, building scope, steward flag, predecessor, reason and expected tenant-control version.

**Result:** Pending replacement or completed transfer with preserved administrator coverage. **Entities:** Invitation, Membership, BuildingGrant, Tenant.

**Errors/business conflicts:** Unaccepted candidate leaves predecessor active. Concurrent last-admin removals allow at most one valid change. Lost administrator recovery requires two-person operator verification and a bounded command.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `administrator.transfer_changed.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-TEN-001

**[UC-TEN-001: Provision a tenant and first administrator](use-cases/UC-TEN-001.md)** · MVP1 · AUTH-02 operator

**Purpose:** onboard an authorized organization with an accountable initial administrator. **Scope:** MFA-protected operator console, provision permission, approved onboarding reference; may set tenant metadata and invite initial steward, not inspect residents.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/platform/tenants` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/platform/tenants/{t}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |

**Key fields:** Organization display/legal names, approved contact email, jurisdiction placeholder, IANA time zone, locale and onboarding reference.

**Result:** Tenant ID and invitation delivery state; acceptance transitions tenant to Active once activation conditions hold. **Entities:** Tenant, Invitation, OperatorAssignment, OutboxMessage.

**Errors/business conflicts:** Duplicate onboarding reference returns existing tenant. Failed email leaves provisioning resumable. Rejected/expired invite never exposes a tenant. Bootstrap acceptance uses UC-IAM-003 with bootstrap-specific checks.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `tenant.provisioned.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-TEN-002

**[UC-TEN-002: Maintain settings and control tenant suspension](use-cases/UC-TEN-002.md)** · MVP1 · AUTH-01 session

**Purpose:** keep operational settings accurate and stop tenant access when necessary. **Scope:** Steward edits settings or requests suspension; only Platform Operator executes suspension/reactivation with verified ticket and MFA. Operator cannot change resident content.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/settings` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `PATCH /api/v1/t/{t}/settings` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/platform/tenants/{t}/state` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Locale, IANA time zone, organization contacts; separate state action with reason, approval reference and ETag.

**Result:** Updated settings or enforced tenant state; suspended UI shows support contact only. **Entities:** Tenant, AuditRecord.

**Errors/business conflicts:** Unauthorized state change returns 403. Suspended jobs pause with explicit status. Stale version returns 412. Failed notification does not undo suspension.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `tenant.state_changed.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-TEN-003

**[UC-TEN-003: Export an organization for handover](use-cases/UC-TEN-003.md)** · MVP1 · AUTH-01 session

**Purpose:** obtain a portable record set for administrator handover or exit. **Scope:** Steward with recent MFA; export restricted to their tenant. Suspended tenants require explicitly approved export-only grant via OP-004; operators do not download the content.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/tenant-exports` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Export purpose, selected record groups and optional as-of filter.

**Result:** Authorized download with manifest, schema version, row counts and snapshot timestamp; no reusable public URL. **Entities:** Tenant, Person, Membership, Issue, IssueComment, AuditRecord.

**Errors/business conflicts:** Mid-stream failure discards incomplete client file; export attempt audit remains. Revocation before stream start denies access; terminate long streams on periodic revocation checks. No cross-tenant identity directory in export.

**Concurrency/idempotency:** Safe read with scoped snapshot; no write ETag required. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-TEN-004

**[UC-TEN-004: Archive and offboard a tenant](use-cases/UC-TEN-004.md)** · MVP1 · AUTH-02 operator

**Purpose:** end service predictably while preserving approved retention obligations. **Scope:** Operator offboard permission, MFA and steward request verified through OP-006; two-person approval for purge. No unrestricted business-data access.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/platform/tenants/{t}/offboarding` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/platform/tenants/{t}/purge-approvals` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Tenant ID, approved exit request, export acknowledgement, retention schedule, legal-hold reference, effective date and ETag.

**Result:** Archived tenant and revocations; purge job is separately authorized and records aggregate counts. **Entities:** Tenant, RetentionHold, DeletionTombstone, AuditRecord.

**Errors/business conflicts:** Hold or missing approval blocks purge. Partial object cleanup in later releases remains retryable and never reactivates tenant. Scheduled purge failure alerts operator; no silent deletion.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `tenant.archived.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-BLD-001

**[UC-BLD-001: Establish and maintain the building register](use-cases/UC-BLD-001.md)** · MVP1 · AUTH-01 session

**Purpose:** identify the buildings and units used to route residents and requests. **Scope:** Steward creates buildings and assigns initial administrator; building-scoped administrators maintain their buildings and units.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/buildings` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/buildings` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `PATCH /api/v1/t/{t}/buildings/{id}` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/buildings/{id}/units` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/buildings/{id}/units` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `PATCH /api/v1/t/{t}/units/{id}` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Building name/address, unit label, optional entrance/floor text, active dates and ETags.

**Result:** Navigable authorized register with stable IDs and preserved references. **Entities:** Building, Unit, BuildingGrant.

**Errors/business conflicts:** Duplicate labels return 409. Attempt to move an occupied or referenced unit to another building is rejected; create replacement and explicit migration later. Cross-tenant references return 404.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-BLD-002

**[UC-BLD-002: Record ownership, move-in, move-out and access dates](use-cases/UC-BLD-002.md)** · MVP1 · AUTH-01 session

**Purpose:** maintain truthful unit history and end access at the correct time. **Scope:** Administrator of affected unit; residents cannot edit legal ownership or other persons. Private contact and evidence are restricted to authorized administrators.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/units/{id}/relationships` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/units/{id}/relationship-changes` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Person, unit, relationship type, effective start/end, source/reason; separate explicit access-grant start/end action.

**Result:** Accurate historical register; ended grants deny current unit and issue access after their effective end. **Entities:** Person, OwnershipInterval, OccupancyInterval, UnitAccessGrant.

**Errors/business conflicts:** Backdated correction requires reason and audit, cannot erase prior activity or silently rewrite MVP3 liability. Transfer does not grant the new owner prior private complaints or former debtor statements. No user is removed from unrelated units.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `unit.relationship_changed.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-BLD-003

**[UC-BLD-003: Preview and commit a register import](use-cases/UC-BLD-003.md)** · MVP2 · AUTH-01 session

**Purpose:** migrate a controlled resident register without bulk access mistakes. **Scope:** Steward or administrator with import permission for all selected buildings; import contains tenant-local person data only.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/register-imports/preview` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/register-imports/{id}/commit` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/register-imports/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |

**Key fields:** Scan-clean sourceAttachmentId for UTF-8 CSV up to 2000 rows and 5 MiB, mapping, source batch ID, dry-run hash and explicit commit.

**Result:** Committed batch with counts and row-level result; invitations remain a separate reviewed action. **Entities:** ImportBatch, Person, Unit, OwnershipInterval, OccupancyInterval.

**Errors/business conflicts:** Stale preview or changed source requires new preview. Duplicate source ID returns prior result. Malformed CSV fails without partial register. Preserve original source only for 7 days in private storage.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-COM-001

**[UC-COM-001: Publish, amend or withdraw a targeted announcement](use-cases/UC-COM-001.md)** · MVP2 · AUTH-01 session

**Purpose:** inform the intended residents with an authoritative, versioned notice. **Scope:** Administrator of every target building/unit; target audience must be within own grants. No publication to global user lists.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/announcements` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/announcements` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `PATCH /api/v1/t/{t}/announcements/{id}` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/announcements/{id}/publish` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/announcements/{id}/withdraw` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Title 160 characters, plain-text body 10000 characters, audience buildings/units, publish/expiry dates, optional clean attachment IDs, expected version.

**Result:** Published announcement or withdrawn version with audit history; delivery is queued. **Entities:** Announcement, AnnouncementVersion, AudienceSnapshot, OutboxMessage.

**Errors/business conflicts:** Invalid audience rejects all targets. Stale draft returns 412. Duplicate publish key returns same version. Email failure never withdraws a successful publication.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `announcement.published.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-COM-002

**[UC-COM-002: Read and acknowledge announcements](use-cases/UC-COM-002.md)** · MVP2 · AUTH-01 session

**Purpose:** find current notices and acknowledge explicitly requested reading. **Scope:** Active membership plus eligible current building/unit grant and announcement audience. Administrator previews only within managed buildings.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/announcements/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `PUT /api/v1/t/{t}/announcements/{id}/acknowledgements/me` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Announcement ID/version; optional explicit acknowledgement command.

**Result:** Notice displayed and optional acknowledged timestamp; newer version shows unacknowledged. **Entities:** AnnouncementVersion, AudienceSnapshot, AnnouncementReceipt.

**Errors/business conflicts:** Expired/withdrawn notice returns 404 with neutral unavailable message. Duplicate acknowledgement returns existing receipt. Revoked audience cannot acknowledge from a stale page.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-COM-003

**[UC-COM-003: Publish and retire building documents](use-cases/UC-COM-003.md)** · MVP2 · AUTH-01 session

**Purpose:** maintain a reliable document library with controlled versions and audiences. **Scope:** Administrator in document building scope; sensitive unit documents require explicit unit audience. No resident directory publication.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/documents` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/documents` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/documents/{id}/versions` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/documents/{id}/retire` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Title, category, clean attachment, audience, effective/expiry dates, version note and ETag.

**Result:** Current document metadata with versioned bytes or retired state. **Entities:** Document, DocumentVersion, Attachment.

**Errors/business conflicts:** Pending/rejected attachment returns 409. A file staged for another tenant/resource is rejected. Concurrent version publish returns 412. Missing object marks version unavailable and alerts support.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `document.published.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-COM-004

**[UC-COM-004: Find and download authorized documents](use-cases/UC-COM-004.md)** · MVP2 · AUTH-01 session

**Purpose:** retrieve a current document without exposing private object URLs. **Scope:** Active membership and current grant matching document audience; administrator within managed building scope. Document visibility never implies access to every file in its tenant.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/documents/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /api/v1/t/{t}/documents/{id}/versions/{version}/content` | Authorized content stream with no-store and refreshed resource permission. |

**Key fields:** Document ID/version, category filter and opaque pagination cursor.

**Result:** Authorized file download or current metadata list; download audit records version and actor. **Entities:** DocumentVersion, Attachment, AuditRecord.

**Errors/business conflicts:** Stale/retired/missing object returns neutral unavailable state; no bucket paths exposed. Partial stream can be retried after fresh authorization. Cross-unit request returns 404.

**Concurrency/idempotency:** Safe read with scoped snapshot; no write ETag required. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-COM-005

**[UC-COM-005: Review notification delivery and retry failures](use-cases/UC-COM-005.md)** · MVP2 · AUTH-01 session

**Purpose:** detect undelivered communication and correct the contact workflow. **Scope:** Administrator sees masked destination and statuses for managed buildings; residents see only own notification inbox. Provider diagnostics and secrets are operator-only.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/deliveries` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/deliveries/{id}/retry` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/notifications/me` | Read scoped representation/list; pagination/filter conventions above apply for collections. |

**Key fields:** Building/status/date filters, delivery ID, retry reason and expected state.

**Result:** Delivery history or queued retry with stable delivery identity and incremented attempt count. **Entities:** Delivery, OutboxMessage, Membership.

**Errors/business conflicts:** Unknown provider outcome enters reconciliation before retry. Removed recipient is suppressed. Repeated retry request does not create parallel sends. Correct email through verified account/contact workflow, not arbitrary destination editing.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `delivery.retry_requested.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-ISS-001

**[UC-ISS-001: Report a building issue](use-cases/UC-ISS-001.md)** · MVP1 · AUTH-01 session

**Purpose:** create a tracked maintenance request or complaint with a visible reference. **Scope:** Active resident with current unit grant; unit issue must use an authorized unit. Common-area issue needs active building access. Administrator may report on behalf of a resident in scope with explicit recorded initiator.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/issues` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Building, optional unit, category request/complaint/incident, title 160 characters, plain text 5000 characters, client request key.

**Result:** Issue reference, Open state, creation time and private conversation link. **Entities:** Issue, IssueTransition, OutboxMessage, AuditRecord.

**Errors/business conflicts:** Invalid location or missing text returns 422. Duplicate request key returns original reference; changed payload under same key returns 409. Email outage does not prevent creation. Closed/archived unit cannot accept new issue.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `issue.created.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-ISS-002

**[UC-ISS-002: Follow an issue and exchange comments](use-cases/UC-ISS-002.md)** · MVP1 · AUTH-01 session

**Purpose:** keep the reporter and responsible administrators in one traceable conversation. **Scope:** Reporter must still have current grant for issue location; administrator must manage that building. Same-unit residents are not automatically participants. No private administrator notes in baseline.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/issues` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /api/v1/t/{t}/issues/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /api/v1/t/{t}/issues/{id}/comments` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/issues/{id}/comments` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Issue ID, comments cursor; optional plain-text comment up to 5000 characters, client key and observed issue version.

**Result:** Ordered private thread and committed comment reference; notification queued for other eligible participants. **Entities:** Issue, IssueComment, OutboxMessage.

**Errors/business conflicts:** Concurrent comment appends are allowed with unique client keys; state changes that disallow commenting cause 409. Revoked reporter cannot read old private thread through this normal route; privacy export is separate.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `issue.comment_added.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-ISS-003

**[UC-ISS-003: Triage and assign an issue](use-cases/UC-ISS-003.md)** · MVP1 · AUTH-01 session

**Purpose:** give each request a accountable owner and visible progress state. **Scope:** Administrator in issue building; assignee must be an active administrator with the same building grant. Residents cannot set priority or assignment.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/issues/{id}/triage` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Issue ID, category, priority low/normal/high, assignee, status Open/InProgress/Waiting and ETag; waiting reason.

**Result:** Updated queue position, assigned administrator and status history. **Entities:** Issue, IssueTransition, Membership, BuildingGrant.

**Errors/business conflicts:** Stale ETag returns 412 without overwrite. Removed assignee returns 422; unassigned open issues remain visible. Closed issues cannot be triaged without reopening.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `issue.triaged.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-ISS-004

**[UC-ISS-004: Resolve an issue and confirm closure](use-cases/UC-ISS-004.md)** · MVP1 · AUTH-01 session

**Purpose:** communicate a concrete resolution and close the request with accountability. **Scope:** Building administrator records resolution; original reporter with current location access confirms closure. Administrator may close after 7 days without reporter response, with explicit reason.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/issues/{id}/resolve` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/issues/{id}/close` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Resolution summary, issue ETag; optional reporter closure acknowledgement or administrator closure reason.

**Result:** Resolved state and summary, or Closed state with actor and closure reason. **Entities:** Issue, IssueTransition, OutboxMessage.

**Errors/business conflicts:** Resolution from stale state returns 412/409. Administrator premature timeout closure is rejected. Failed email leaves Resolved committed and visible in portal.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `issue.resolution_changed.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-ISS-005

**[UC-ISS-005: Reopen an unresolved problem](use-cases/UC-ISS-005.md)** · MVP1 · AUTH-01 session

**Purpose:** restore follow-up when a recorded resolution did not solve the problem. **Scope:** Original reporter with current location grant or administrator of issue building.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/issues/{id}/reopen` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Issue ID, reason up to 2000 characters, ETag and idempotency key.

**Result:** Open issue with incremented version and complete earlier resolution history. **Entities:** Issue, IssueTransition, IssueComment.

**Errors/business conflicts:** Already open returns 409 unless same request replay. Outside window returns 422 with new-issue path. Removed resident cannot reopen after moving out.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `issue.reopened.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-ISS-006

**[UC-ISS-006: Add and access issue evidence](use-cases/UC-ISS-006.md)** · MVP2 · AUTH-01 session

**Purpose:** provide photos or documents that clarify an authorized issue. **Scope:** Issue reporter with current grant or scoped administrator may attach; viewers need current parent-resource authorization. Uploader alone does not authorize access after move-out.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/attachments` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `PUT /api/v1/t/{t}/attachments/{id}/content` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/attachments/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /api/v1/t/{t}/attachments/{id}/content` | Authorized content stream with no-store and refreshed resource permission. |
| `POST /api/v1/t/{t}/attachments/{id}/detach` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Parent issue, file bytes, declared type, length, checksum; attachment ID for download or removal.

**Result:** Pending attachment reference then Clean or Rejected status; authorized clean bytes downloadable through backend. **Entities:** Attachment, Issue, OutboxMessage, AuditRecord.

**Errors/business conflicts:** Exceeded quota/type rejects before storage. Storage timeout leaves Pending and supports safe retry by upload ID/checksum; never attach unknown bytes. Scanner outage leaves inaccessible PendingScan. Metadata/byte orphan cleanup runs after 24 hours.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `attachment.scan_requested.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-MNT-001

**[UC-MNT-001: Commission and complete a work order](use-cases/UC-MNT-001.md)** · MVP4 · AUTH-01 session

**Purpose:** coordinate actual repair work and connect completion to a resident issue. **Scope:** Administrator for work-order building. Contractor is a contact record, not a portal user; share only necessary task details using verified manual contact.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/work-orders` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/work-orders` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/work-orders/{id}/transitions` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/contractors` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/contractors` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Issue or standalone task, location, contractor contact, scope, planned dates, estimate, assignee and ETag.

**Result:** Traceable commissioned work and completion summary; resident issue waits for separate resolution decision. **Entities:** WorkOrder, Contractor, Issue, Asset.

**Errors/business conflicts:** Stale change rejected. Contractor details from another tenant prohibited. Over-budget approval requires policy-specific approval recorded before work; thresholds must be validated before MVP4.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `work_order.state_changed.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-MNT-002

**[UC-MNT-002: Maintain shared assets and locations](use-cases/UC-MNT-002.md)** · MVP4 · AUTH-01 session

**Purpose:** keep service history tied to a known common asset. **Scope:** Administrator of asset building; asset technical details are not resident-visible unless deliberately published as a document.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/assets` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/assets` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `PATCH /api/v1/t/{t}/assets/{id}` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/common-areas` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/common-areas` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Common area name, asset tag, category, serial, installation/retirement dates, warranty reference and ETag.

**Result:** Current asset register and retained service history. **Entities:** Asset, CommonArea, WorkOrder.

**Errors/business conflicts:** Duplicate asset tag returns 409. Retirement before completed work date requires corrected date or explained historical correction. Foreign building reference returns 404.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-MNT-003

**[UC-MNT-003: Schedule preventive maintenance and generate due work](use-cases/UC-MNT-003.md)** · MVP4 · AUTH-01 session

**Purpose:** avoid missing recurring maintenance without generating duplicate work. **Scope:** Administrator configures schedule in managed building; due worker uses one tenant-scoped service job with explicit asset scope.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/maintenance-schedules` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/maintenance-schedules` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `PATCH /api/v1/t/{t}/maintenance-schedules/{id}` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `JOB maintenance.generate-due.v1` | Internal due-job envelope; no public HTTP route, cookie or CSRF. |

**Key fields:** Asset, recurrence calendar/month interval, local time zone, next due date, template, activation/end dates and ETag.

**Result:** Active schedule and uniquely identified due drafts visible to administrator. **Entities:** MaintenanceSchedule, WorkOrder, Job, OutboxMessage.

**Errors/business conflicts:** Worker retry cannot duplicate occurrence. Suspended tenant or retired asset pauses schedule with reason. Changed schedule applies to future occurrences; existing drafts require explicit cancellation.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `maintenance.work_due.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-MNT-004

**[UC-MNT-004: Record and correct maintenance expenses](use-cases/UC-MNT-004.md)** · MVP4 · AUTH-01 session

**Purpose:** record actual repair cost and provide a controlled handoff to finance. **Scope:** Administrator with finance permission in expense building; residents see only approved aggregates or their allocated charges, never contractor bank details.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/expenses` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/expenses` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/expenses/{id}/approve` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/expenses/{id}/correct` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Work order optional, vendor reference, receipt attachment, amount/currency, service date, cost category and correction reason.

**Result:** Expense history with remaining allocatable amount and explicit finance handoff status. **Entities:** Expense, ExpenseCorrection, WorkOrder, Attachment, ChargeSource.

**Errors/business conflicts:** Duplicate invoice reference is flagged for review; not automatically rejected across different vendors. Currency mismatch rejected. Reversal of already allocated cost requires FIN-008 charge correction before allocation changes.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `expense.approved.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-FIN-001

**[UC-FIN-001: Establish debtor accounts and opening balances](use-cases/UC-FIN-001.md)** · MVP3 · AUTH-01 session

**Purpose:** start financial transparency from reconciled liabilities rather than ambiguous unit totals. **Scope:** Administrator with finance permission and building scope. Residents see only accounts with explicit AccountAccessGrant; unit access or ownership alone does not grant financial access.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/billing-accounts` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/billing-accounts` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/billing-accounts/{id}/opening-balance` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/billing-accounts/{id}/access-grants` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Unit, liable party or explicitly agreed joint party, liability interval, currency, cutover date, source reference, signed opening amount in minor units and reviewer acknowledgement.

**Result:** Open account with immutable opening journal, traceable source and explicit account-access grants. **Entities:** BillingAccount, LiabilityParty, LiabilityPartyMember, AccountAccessGrant, Journal, JournalLine, LedgerAccount.

**Errors/business conflicts:** Unreconciled source blocks posting. Same cutover/source/account cannot post twice. Ambiguous co-owner liability blocks activation until stakeholder decision. No inferred splitting of debt at move-out.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `finance.opening_posted.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-FIN-002

**[UC-FIN-002: Define charge rules and billing periods](use-cases/UC-FIN-002.md)** · MVP3 · AUTH-01 session

**Purpose:** approve transparent dues and allocation rules before charging residents. **Scope:** Finance-enabled administrator for every included building; multi-building rules require steward plus finance permission.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/billing-periods` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/billing-periods` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/charge-rules` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/charge-rules` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/charge-rules/{id}/activate` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Period dates/due date, charge type, fixed amount or pool amount, equal/unit-area/approved-share weights, effective interval, recurrence option, rule version.

**Result:** Validated rule version and Open period available for preview or recurring drafts. **Entities:** BillingPeriod, ChargeRuleVersion, AllocationBasis.

**Errors/business conflicts:** Overlap/conflicting rule version or missing weight prevents activation. Closed periods cannot accept new rules retroactively. No late-fee or tax automation in baseline.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-FIN-003

**[UC-FIN-003: Preview and post a charge batch](use-cases/UC-FIN-003.md)** · MVP3 · AUTH-01 session

**Purpose:** issue reproducible charges with no partial billing. **Scope:** Finance-enabled administrator over every account/building in the batch; posting requires recent MFA and reviewed preview digest.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/charge-batches/preview` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/charge-batches/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/charge-batches/{id}/post` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Period, rule versions/source pools, effective account set, preview digest, ETags and posting key.

**Result:** Posted batch with per-account amounts and statement-ready balances; notification queued. **Entities:** ChargeBatch, Charge, Journal, JournalLine, BillingPeriod.

**Errors/business conflicts:** Stale preview returns 409 and new preview required. Any invalid account aborts whole batch. Network timeout uses posting key/status lookup, never a fresh blind posting. Batch size capped at 2000 accounts; larger organizations use reviewed building batches.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `finance.charges_posted.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-FIN-004

**[UC-FIN-004: View account activity and statements](use-cases/UC-FIN-004.md)** · MVP3 · AUTH-01 session

**Purpose:** understand exactly what is owed, paid and still open. **Scope:** Resident with effective explicit AccountAccessGrant for this liability account; administrator with finance permission and building scope. No visibility of other residents arrears.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/billing-accounts/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /api/v1/t/{t}/billing-accounts/{id}/activity` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /api/v1/t/{t}/billing-accounts/{id}/statements` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /api/v1/t/{t}/statements/{id}/content` | Authorized content stream with no-store and refreshed resource permission. |

**Key fields:** Account ID, period/as-of timestamp and cursor; optional statement download.

**Result:** Accessible activity page or versioned statement document with reconciled balance. **Entities:** BillingAccount, AccountAccessGrant, Statement, JournalLine, PaymentAllocation.

**Errors/business conflicts:** Unknown/unauthorized account returns 404. Data disagreement blocks statement issuance and alerts finance owner; no guessed balance. Download rechecks authorization.

**Concurrency/idempotency:** Safe read with scoped snapshot; no write ETag required. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-FIN-005

**[UC-FIN-005: Record an externally received payment](use-cases/UC-FIN-005.md)** · MVP3 · AUTH-01 session

**Purpose:** recognize money already received through bank or cash channels. **Scope:** Finance-enabled administrator in account building; no resident self-asserted payment creates a ledger posting.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/payments` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/payments` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Amount minor units, currency, received date, bank/cash channel, source transaction reference, debtor account if known and evidence.

**Result:** Posted receipt and unapplied account credit or unmatched suspense item. **Entities:** Payment, Journal, JournalLine, LedgerAccount.

**Errors/business conflicts:** Duplicate reference returns existing payment if identical, conflict if amount differs. Closed-period received date is retained but posting date uses open period with reason. Unknown cash source needs controlled manual receipt number.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `finance.payment_recorded.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-FIN-006

**[UC-FIN-006: Allocate payments and match suspense receipts](use-cases/UC-FIN-006.md)** · MVP3 · AUTH-01 session

**Purpose:** apply paid funds to the correct debtor and outstanding charges. **Scope:** Finance-enabled administrator over payment and destination account buildings; cross-tenant or cross-currency movement prohibited.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/payments/{id}/allocations` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/payments/{id}/match` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/allocations/{id}/reverse` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Payment ID, account, selected charges, allocated amounts, matching evidence, expected payment/account versions.

**Result:** Reconciled outstanding charges and residual credit with complete allocation history. **Entities:** Payment, PaymentAllocation, AllocationReversal, Journal.

**Errors/business conflicts:** Concurrent allocator gets 409/412 and recomputes. Different debtor requires explicit verified match or FIN-008 transfer correction, never editing payment owner. Disputed evidence leaves receipt unmatched.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `finance.payment_allocated.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-FIN-007

**[UC-FIN-007: Issue a credit or reasoned balance adjustment](use-cases/UC-FIN-007.md)** · MVP3 · AUTH-01 session

**Purpose:** correct liability through visible financial entries. **Scope:** Finance-enabled administrator in account scope; recent MFA and approval reference for adjustment. No resident permission.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/billing-accounts/{id}/adjustments` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Account, signed amount, original charge optional, effective date, reason code, explanation and supporting reference.

**Result:** Immutable adjustment linked to reason and source; account statement reflects it. **Entities:** Adjustment, Journal, JournalLine, ApprovalRecord.

**Errors/business conflicts:** Missing reason/approval blocks posting. Duplicate command key returns original journal. Credit against settled charge creates unapplied account credit unless explicitly reallocated.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `finance.adjustment_posted.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-FIN-008

**[UC-FIN-008: Reverse erroneous postings or record external refunds](use-cases/UC-FIN-008.md)** · MVP3 · AUTH-01 session

**Purpose:** correct errors without erasing the accounting trail. **Scope:** Finance-enabled administrator over all affected accounts, recent MFA, approved reason and second review where policy requires.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/postings/{id}/reversals` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/payments/{id}/refund-records` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Original posting/payment, full or supported partial amount, reason, replacement reference or external refund evidence, posting period.

**Result:** Original immutable posting plus linked compensation; outstanding balance recalculated from ledger. **Entities:** Reversal, RefundRecord, Journal, JournalLine, AllocationReversal.

**Errors/business conflicts:** Insufficient reversible amount returns 409. Already fully reversed returns existing result for same key. Attempt to delete original is unsupported. Uncertain external refund must stay unposted pending evidence.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `finance.posting_reversed.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-FIN-009

**[UC-FIN-009: Reconcile receipts and close a billing period](use-cases/UC-FIN-009.md)** · MVP3 · AUTH-01 session

**Purpose:** confirm recorded money against external evidence and freeze an agreed period. **Scope:** Finance-enabled administrator for reconciliation building scope; tenant-period closure requires steward plus finance permission.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/reconciliations/preview` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/reconciliations/{id}/confirm` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/billing-periods/{id}/close` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Scan-clean bank-statement sourceAttachmentId or manually entered control totals, cash control totals, date interval, discrepancy decisions, closure ETag and reviewer acknowledgement.

**Result:** Closed period with control totals, reviewer, frozen account statement snapshots and explicit unresolved-exception register. **Entities:** ReconciliationBatch, ReconciliationItem, BillingPeriod, Journal.

**Errors/business conflicts:** Posting race waits on period lock then either commits before closure or is rejected. Duplicate bank rows cannot create receipts. Unexplained difference blocks closure. File scan failure prevents import.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `finance.period_closed.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-FIN-010

**[UC-FIN-010: Review arrears and send private reminders](use-cases/UC-FIN-010.md)** · MVP3 · AUTH-01 session

**Purpose:** follow up overdue balances without exposing debt to neighbors. **Scope:** Finance-enabled administrator in account scope; each resident receives only their account reminder. No public arrears list.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/arrears` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/arrears/reminders` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** As-of date, building, overdue age bands, minimum amount, selected accounts and reminder preview.

**Result:** As-of arrears report and delivery history for selected eligible debtors. **Entities:** Charge, PaymentAllocation, Adjustment, Reminder, Delivery.

**Errors/business conflicts:** Stale balances at dispatch trigger recalculation; paid accounts are suppressed. Missing verified contact leaves manual follow-up item. Suspended account access prevents automated disclosure.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `finance.reminder_requested.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-FIN-011

**[UC-FIN-011: Generate recurring charge drafts](use-cases/UC-FIN-011.md)** · MVP3 · service job identity

**Purpose:** prepare each due billing batch once for administrator review. **Scope:** Internal worker identity; scheduler enumerates only due job metadata then processes one authorized active tenant/rule per transaction. No human session is fabricated.

| Operation | Inputs/output and behavior |
| --- | --- |
| `JOB finance.generate-drafts.v1` | Internal due-job envelope; no public HTTP route, cookie or CSRF. |
| `GET /api/v1/t/{t}/charge-batches?status=Draft` | Read scoped representation/list; pagination/filter conventions above apply for collections. |

**Key fields:** Job envelope with tenantId, ruleVersionId, occurrence local date, eventId and attempt.

**Result:** Exactly one stored draft per occurrence with QueuedReview or Blocked status; no financial posting. **Entities:** Job, ChargeRuleVersion, ChargeBatch, BillingPeriod.

**Errors/business conflicts:** Duplicate delivery returns existing draft. Worker crash after commit is safe. Suspended tenant pauses. Changed rule versions apply only from explicit effective date.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `finance.draft_generated.v1`. **Pagination:** not applicable to the job itself; administrator review lists use common convention.

## API-UTL-001

**[UC-UTL-001: Register meters and effective associations](use-cases/UC-UTL-001.md)** · MVP4 · AUTH-01 session

**Purpose:** attribute readings to the correct unit or common supply over time. **Scope:** Administrator of meter building; residents can view only meters currently assigned to their accessible unit. Physical ownership of meter is descriptive and does not grant app access.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/meters` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/meters` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/meters/{id}/association-changes` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Meter serial, utility type, measurement unit, precision, installation/retirement dates, unit/common-area association interval, starting value and replacement link.

**Result:** Effective meter history and validated baseline. **Entities:** Meter, MeterAssociation, MeterReading.

**Errors/business conflicts:** Overlap returns 409. Backdated reassociation affecting billed usage requires explicit correction plan, not automatic reallocation. Cross-building association outside admin scope denied.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-UTL-002

**[UC-UTL-002: Submit a meter reading](use-cases/UC-UTL-002.md)** · MVP4 · AUTH-01 session

**Purpose:** report consumption evidence for an eligible meter and period. **Scope:** Resident with current unit grant and meter association valid at reading time; administrator may enter on behalf with attribution. Historical reading scope does not restore ended app access.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/reading-windows` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/meter-readings` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/meter-readings/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |

**Key fields:** Meter, reading timestamp, decimal register value as string, optional clean photo, reading-window ID and client key.

**Result:** Pending reading reference visible to submitter and scoped administrator. **Entities:** MeterReading, Meter, ReadingWindow, Attachment.

**Errors/business conflicts:** Duplicate meter/window/value returns previous result; conflicting second reading requires correction proposal. Closed window rejects unless administrator exception with reason. Invalid photo remains unbound.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `meter.reading_submitted.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-UTL-003

**[UC-UTL-003: Validate or correct meter readings](use-cases/UC-UTL-003.md)** · MVP4 · AUTH-01 session

**Purpose:** approve reliable readings and preserve corrections after use. **Scope:** Administrator for meter building; residents propose correction by resubmission with supersedes reference but cannot approve.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/meter-readings` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/meter-readings/{id}/decisions` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Reading ID, approve/reject/supersede action, reason, replacement value, evidence and ETag.

**Result:** Approved current reading or rejected proposal with original history retained. **Entities:** MeterReading, ReadingDecision, ConsumptionBatch, ChargeSource.

**Errors/business conflicts:** Concurrent approval returns 412. Missing rollover evidence blocks acceptance. Already billed source produces correction-required state and prevents silent rebilling.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `meter.reading_validated.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-UTL-004

**[UC-UTL-004: Publish tariffs and shared allocation rules](use-cases/UC-UTL-004.md)** · MVP4 · AUTH-01 session

**Purpose:** establish explainable utility pricing before consumption is charged. **Scope:** Finance-enabled administrator for all affected buildings and meters.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/tariffs` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/tariffs` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/tariffs/{id}/activate` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Utility, tariff unit rate decimal, currency, effective interval, fixed component if any, shared-loss policy and approved allocation basis.

**Result:** Published reproducible utility pricing rule. **Entities:** TariffVersion, SharedAllocationRule, AllocationBasis.

**Errors/business conflicts:** Incompatible measurement units/currency rejected. Missing total/shared meter or weights blocks activation. Retroactive changes create new version and correction plan.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-UTL-005

**[UC-UTL-005: Calculate consumption and hand off charges](use-cases/UC-UTL-005.md)** · MVP4 · AUTH-01 session

**Purpose:** convert accepted measurements into reviewed, traceable billing input. **Scope:** Finance-enabled administrator for every meter/account in batch; residents only see approved own consumption via account or meter views.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/consumption-batches/preview` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/consumption-batches/{id}/handoff` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/consumption-batches/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |

**Key fields:** Period, accepted reading versions, tariff versions, shared rule, preview digest and handoff key.

**Result:** Versioned consumption breakdown and finance draft linked to exact evidence. **Entities:** ConsumptionBatch, ConsumptionLine, ChargeSource, ChargeBatch.

**Errors/business conflicts:** Stale source blocks commit and requires recalculation. Same handoff replay returns existing draft. Correction of posted usage creates differential or reversal/rebill proposal approved through finance.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `utility.billing_draft_created.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-GOV-001

**[UC-GOV-001: Organize a meeting and publish minutes](use-cases/UC-GOV-001.md)** · MVP5 · AUTH-01 session

**Purpose:** give residents one place for meeting agenda, location and approved minutes. **Scope:** Administrator of all target buildings; residents read only meetings targeted to current building membership. Attendance details restricted to organizer.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/meetings` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /api/v1/t/{t}/meetings/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/meetings` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/meetings/{id}/publish` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/meetings/{id}/minutes` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/meetings/{id}/cancel` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Title, agenda, location or approved URL, local date/time/zone, audience, draft minutes and clean attachments, ETag.

**Result:** Current meeting page and versioned minutes available to eligible residents. **Entities:** Meeting, MeetingVersion, DocumentVersion, AudienceSnapshot.

**Errors/business conflicts:** Invalid time zone or DST ambiguity requires explicit offset. Cancelled meeting retains cancellation notice. Replacing minutes keeps prior restricted version. External meeting URL is allowlisted/safely rendered, never fetched by backend.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `meeting.published.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-GOV-002

**[UC-GOV-002: Create and close an informal poll](use-cases/UC-GOV-002.md)** · MVP5 · AUTH-01 session

**Purpose:** collect advisory community preferences with transparent participation rules. **Scope:** Administrator for poll building audience; results show aggregates only, except restricted audit identities. No formal binding voting authority claimed.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/polls` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/polls` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/polls/{id}/open` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/polls/{id}/close` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/polls/{id}/results` | Read scoped representation/list; pagination/filter conventions above apply for collections. |

**Key fields:** Question, 2-10 options, opening/closing instants, eligible building audience, optional participation target and ETag.

**Result:** Open advisory poll or closed aggregate result with eligible count, votes and participation percentage. **Entities:** Poll, PollOption, PollEligibility, Ballot.

**Errors/business conflicts:** Question edit after opening rejected. Revoked member before voting cannot participate; votes cast while eligible retained unless poll invalidated by recorded correction policy. Early closure requires reason. No secret-ballot guarantee because deduplication retains restricted linkage.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `poll.state_changed.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-GOV-003

**[UC-GOV-003: Cast an advisory poll ballot](use-cases/UC-GOV-003.md)** · MVP5 · AUTH-01 session

**Purpose:** express a community preference once under the stated eligibility rule. **Scope:** Active membership on opening eligibility snapshot and current target-building grant. Administrator can vote only through their eligible membership.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/polls/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/polls/{id}/ballots` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/polls/{id}/ballots/me` | Read scoped representation/list; pagination/filter conventions above apply for collections. |

**Key fields:** Poll ID, option ID, idempotency key and displayed poll version.

**Result:** Own ballot receipt with timestamp; aggregate results appear only after closure. **Entities:** Poll, PollEligibility, Ballot.

**Errors/business conflicts:** Duplicate same-key vote returns receipt; different choice after vote returns 409. Closed poll or revoked grant denies. Counter updates and ballot commit together.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `poll.ballot_cast.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-RES-001

**[UC-RES-001: Define shared-resource availability](use-cases/UC-RES-001.md)** · MVP5 · AUTH-01 session

**Purpose:** publish fair booking windows for common facilities. **Scope:** Administrator of resource building; residents see availability without other bookers identities.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/resources` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/resources` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `PUT /api/v1/t/{t}/resources/{id}/availability` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `POST /api/v1/t/{t}/resources/{id}/blocks` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Resource, location, time zone, opening hours, capacity-one slot rules, duration/minimum notice, block interval and ETag.

**Result:** Bookable intervals or blocked resource with affected reservations explicitly handled. **Entities:** SharedResource, AvailabilityRule, ResourceBlock, Reservation.

**Errors/business conflicts:** Overlapping block and live reservation returns 409 plus scoped conflict list for administrator. DST ambiguity requires explicit occurrence offset. Retired resource forbids future bookings.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `resource.availability_changed.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-RES-002

**[UC-RES-002: Reserve or cancel a shared facility](use-cases/UC-RES-002.md)** · MVP5 · AUTH-01 session

**Purpose:** book an available resource without competing reservations for the same time. **Scope:** Current resident grant for resource building; creator can cancel own reservation, administrator can cancel within building with reason.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/resources/{id}/availability` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/reservations` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/reservations/me` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/reservations/{id}/cancel` | Command with validated fields below; state changes and business constraints enforced before commit. |

**Key fields:** Resource ID, start/end instants with displayed local zone, client key; reservation version for cancellation.

**Result:** Confirmed reservation reference or cancelled state with availability released. **Entities:** Reservation, SharedResource, ResourceBlock.

**Errors/business conflicts:** Concurrent conflicting booking returns 409 and refreshed availability. Duplicate key returns own existing reservation. Cancellation is idempotent. No email dependency for confirmation.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `reservation.state_changed.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-RPT-001

**[UC-RPT-001: Review a role-scoped operational dashboard](use-cases/UC-RPT-001.md)** · MVP1 · AUTH-01 session

**Purpose:** know which issue needs action and whether reported problems are progressing. **Scope:** Resident sees own authorized issues only; administrator sees managed-building queues and aggregates. Tenant steward sees cross-building totals within tenant. Operator sees service metrics only.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/dashboard` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /api/v1/t/{t}/issues` | Read scoped representation/list; pagination/filter conventions above apply for collections. |

**Key fields:** Building scope, issue state, date interval, assigned-to-me filter and opaque cursor.

**Result:** Consistent dashboard with empty/loading/error states and authorized deep links. **Entities:** Issue, Membership, BuildingGrant, UnitAccessGrant.

**Errors/business conflicts:** Unsupported filter returns 422. Stale count is labeled as-of if paginated separately. No leakage through total counts, search suggestions or guessed tenant names. Read retries safe.

**Concurrency/idempotency:** Safe read with scoped snapshot; no write ETag required. **Events:** `None`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-RPT-002

**[UC-RPT-002: Generate scoped management reports and exports](use-cases/UC-RPT-002.md)** · MVP3 · AUTH-01 session

**Purpose:** produce auditable operational and financial reports for authorized review. **Scope:** Administrator building scope; finance data additionally requires finance permission. Export requester must retain scope at generation and download. Personal residents use own statements, not management export.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/t/{t}/reports` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/t/{t}/reports/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /api/v1/t/{t}/reports/{id}/content` | Authorized content stream with no-store and refreshed resource permission. |

**Key fields:** Report type issues/receivables/receipts/reconciliation, building/date filters, format CSV/Markdown and client key.

**Result:** 202 job URL then Ready/Failed/Suppressed status and authorized download when ready. **Entities:** ExportJob, Attachment, Issue, Journal, AuditRecord.

**Errors/business conflicts:** Revocation before execution suppresses report; revocation after generation denies download and schedules deletion. Worker retry writes deterministic version key and avoids duplicate published outputs. Snapshot failure retries whole report, not mixed snapshots.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `report.requested.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-PRV-001

**[UC-PRV-001: Request personal data access, correction or deletion review](use-cases/UC-PRV-001.md)** · MVP2 · AUTH-04 subject/case

**Purpose:** ask the responsible organization to address personal-data rights or errors. **Scope:** Authenticated current or former identity may submit for a former tenant through restricted own-membership directory; no ordinary tenant access is restored. Offline identity-verified support remains available.

| Operation | Inputs/output and behavior |
| --- | --- |
| `POST /api/v1/privacy-cases` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/privacy-cases/me` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `GET /api/v1/privacy-cases/{id}` | Read scoped representation/list; pagination/filter conventions above apply for collections. |

**Key fields:** Request type access/correction/deletion, tenant, own identity, concise description, contact preference and client key.

**Result:** Case reference and own status view; business access permissions unchanged. **Entities:** PrivacyCase, Membership, UserIdentity.

**Errors/business conflicts:** Unrelated tenant request returns neutral support route. Sensitive attachments use approved later secure process, not email. Duplicate submission returns same case. Tenant archived routes to retained controller contact/operator metadata queue.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `privacy.case_requested.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.

## API-PRV-002

**[UC-PRV-002: Review and fulfill a privacy case](use-cases/UC-PRV-002.md)** · MVP2 · AUTH-01 session

**Purpose:** provide justified data access or correction while protecting third parties and retained records. **Scope:** Designated privacy permission with tenant-steward accountability; object-level review across buildings explicitly approved. Operator assists only with bounded export/redaction job and two-person ticket, without unrestricted reading.

| Operation | Inputs/output and behavior |
| --- | --- |
| `GET /api/v1/t/{t}/privacy-cases` | Read scoped representation/list; pagination/filter conventions above apply for collections. |
| `POST /api/v1/t/{t}/privacy-cases/{id}/decisions` | Command with validated fields below; state changes and business constraints enforced before commit. |
| `GET /api/v1/privacy-cases/{id}/response` | Authorized content stream with no-store and refreshed resource permission. |

**Key fields:** Case ID, verified identity evidence reference, decision/legal-hold basis, reviewed data scope, redaction plan, outcome and ETag.

**Result:** Fulfilled or reasoned deferred/rejected case with audit and subject-only response package if approved. **Entities:** PrivacyCase, PrivacyAction, RetentionHold, Attachment, AuditRecord.

**Errors/business conflicts:** Unverified requester or third-party data blocks release. Legal hold yields documented partial fulfillment. Concurrent decision returns 412. Export/scanning failure leaves Processing and retries without duplicate disclosure.

**Concurrency/idempotency:** CON-01; ETag for edits, command key for creates/appends, source/occurrence uniqueness for irreversible effects. **Events:** `privacy.case_decided.v1`. **Pagination:** common convention on all listed collections; scalar commands and downloads are not paginated.
