# UC-COM-001 — Publish, amend or withdraw a targeted announcement

[Catalogue](../use-case-catalog.md) · [Architecture](../solution-design.md) · [Shared contracts](../shared-patterns.md)

## 1. Identity and references

**ID:** UC-COM-001. **Module:** Communication. **Release:** MVP2. **Requirement references:** BR-04. **API family:** API-COM-001. **Screen:** S-10. **Status:** proposed implementation contract; business rules require the listed stakeholder validation.

## 2. Business objective and user story

As a building administrator, I want to inform the intended residents with an authoritative, versioned notice, so the organization can complete this goal with a traceable result. This release implements the stated goal only; future module dependencies are not implied.

## 3. Actors

**Primary:** Building Administrator. **Supporting:** Frontend and Backend API; PostgreSQL persists or retrieves the authorized business state.  Email Provider is an asynchronous supporting system under REL-01.

## 4. Trigger and preconditions

**Trigger:** The primary actor initiates the named action from S-10. Dependencies/capabilities: [UC-ISS-006](UC-ISS-006.md); [UC-IAM-005](UC-IAM-005.md). Required referenced records must exist in the authorized scope; the target state must permit this action. Ordinary tenant activity requires Active tenant and effective membership; authorized export/offboarding exceptions follow the narrow operational procedures.

## 5. Authorization

Administrator of every target building/unit; target audience must be within own grants. No publication to global user lists. Apply **AUTH-01**, effective dates and server-side resource checks from [shared-patterns.md](../shared-patterns.md). UI visibility is a convenience only. Any cached/delayed action is reauthorized before use.

## 6. Inputs, validation and business rules

**Inputs:** Title 160 characters, plain-text body 10000 characters, audience buildings/units, publish/expiry dates, optional clean attachment IDs, expected version.

**Rules:** Only published versions are resident-visible. Audience at publication is snapshotted as eligible membership IDs; access at view and delivery also requires current grants. New residents see current building-wide notices marked available-to-new-members; private unit notices require explicit targeting. Amendments produce new version; withdrawal hides content.

Text is treated as data; enforce lengths and allowlists server-side. IDs are opaque and must resolve through authorized relationships. No client-supplied role, tenant label or object key establishes permission.

## 7. Main success flow

1. **User:** opens S-10 and initiates “Publish, amend or withdraw a targeted announcement” with the inputs above.
2. **Frontend:** collects only relevant fields, validates shape, shows the active tenant and submits the listed API operation; protected writes carry session, CSRF, context version and applicable request/ETag values.
3. **Backend:** applies AUTH-01; Administrator of every target building/unit; target audience must be within own grants. No publication to global user lists.
4. **Backend → Database:** reads Announcement draft, target units and active grant-derived audience through the authorized scope; evaluates the workflow-specific rules in section 6.
5. **Backend:** Publish a version and persist eligible notification recipients. Persistence enforces invariants and records the result and audit; any event is committed through REL-01.
6. **Integration:** After commit queue one link-only message per eligible identity and version; current authorization is rechecked by REL-01. No other external provider call is required for the synchronous business result.
7. **Backend → Frontend → User:** Published announcement or withdrawn version with audit history; delivery is queued. Preserve safe user input on a recoverable error and show the returned state/version.

## 8. Alternatives and exceptions

Invalid audience rejects all targets. Stale draft returns 412. Duplicate publish key returns same version. Email failure never withdraws a successful publication.

ERR-01 applies: malformed input is 400/422; missing authentication is 401; disallowed role is 403; invisible resource is 404. Never reveal another tenant through constraint names, counts or error details. Stale edits require reload/review; identical successful command replay returns its earlier result after fresh authorization. Database failure rolls back the domain write and its outbox; the UI must query the command outcome before resubmitting an uncertain operation.

## 9. Postconditions and failure guarantees

**Success:** Published announcement or withdrawn version with audit history; delivery is queued. **Failure:** rejected authorization or validation does not change domain records. Only committed state is authoritative; audit and outbox do not announce a rolled-back change. External side effects can fail after commit and are reconciled under REL-01.

## 10. Data, APIs and events

**Entities:** `Announcement`; `AnnouncementVersion`; `AudienceSnapshot`; `OutboxMessage`; definitions in [data-model.md](../data-model.md). **API operations:** GET /api/v1/t/{t}/announcements; POST /api/v1/t/{t}/announcements; PATCH /api/v1/t/{t}/announcements/{id}; POST /api/v1/t/{t}/announcements/{id}/publish; POST /api/v1/t/{t}/announcements/{id}/withdraw. Contracts and errors: [API-COM-001](../api-catalog.md#api-com-001). **Business event:** `announcement.published.v1`. Envelope, payload whitelist, deduplication and dispatch follow REL-01; the event name does not itself require an external message broker.

## 11. Transaction, consistency and retries

Use CON-01: authorize first, validate current state inside the transaction, apply tenant-aware constraints, and commit the business change with its audit and any outbox records. State updates require If-Match; append/create commands use durable business uniqueness plus a request key. Same-key same-payload replay returns the stored outcome; different payload returns 409. Never retry a state change with a new key after an uncertain response.

## 12. Audit and notifications

Record action, actor/service identity, tenant where applicable, resource ID, safe transition fields, reason reference, timestamp and correlation ID. After commit queue one link-only message per eligible identity and version; current authorization is rechecked by REL-01. Never log tokens, raw emails, phone numbers, issue/comment bodies, financial narrative, ballot choices or file bytes. Sensitive business evidence remains in authorized records, not telemetry. Finance audit includes posting IDs and control totals, never editable history.

## 13. Acceptance criteria

- **AT-UC-COM-001-01 — Outcome:** Given the stated actor, scope and valid inputs, when this workflow succeeds, then published announcement or withdrawn version with audit history; delivery is queued.
- **AT-UC-COM-001-02 — Business boundary:** Given a queued recipient moves out before dispatch, when this workflow is exercised, then the worker suppresses the announcement email and the portal denies access.
- **AT-UC-COM-001-03 — Authorization:** Given the caller lacks the required tenant/building/unit or resource scope, when a known ID is substituted in this workflow, then the API denies access with no protected content, domain mutation or notification.
- **AT-UC-COM-001-04 — Failure:** Given validation fails or the database transaction aborts, when the client checks the outcome, then no partial domain change or queued external side effect is reported as successful.

The cross-scope test applies both to a second independent tenant and to a denied building/unit within the same tenant where that resource exists. Execution evidence belongs in the release gate; the design itself is not a test result.

## 14. End-to-end sequence

```mermaid
sequenceDiagram
    autonumber
    actor U as Building Administrator
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    U->>F: Publish, amend or withdraw a targeted announcement
    F->>A: POST /api/v1/t/{t}/announcements/{id}/publish with session and CSRF
    A->>D: AUTH-01 validate session and active tenant membership
    D-->>A: Role, building grants, effective scope and context version
    A->>A: Authorize action and resource scope before domain access
    alt Authentication or scope denied
        A-->>F: 401, 403 or concealed 404, no domain write
        F-->>U: Sign-in or unavailable action
    else Authorized context
        A->>D: BEGIN scoped transaction and acquire required control locks
        A->>D: Scoped read of Announcement draft, target units and active grant-derived audience
        D-->>A: Authorized records and versions
        A->>A: Validate business rules, expected version and command key
        alt Invalid, stale or duplicate conflict
            A->>D: ROLLBACK with no domain side effect
            A-->>F: 422, 412 or 409, refresh or correct input
        else Valid command
            A->>D: Publish a version and persist eligible notification recipients
            A->>D: Enforce tenant keys and invariants, append audit and outbox
            A->>D: COMMIT domain result and command deduplication
            A-->>F: 200 or 201 committed result and current version
        end
        F-->>U: Show returned outcome for publish, amend or withdraw a targeted announcement or actionable error
    end
    Note over A,D: External delivery happens after commit under REL-01, no atomic provider commit
```

### Notification continuation for UC-COM-001

The business result above is already committed. This continuation is initiated by the hosted worker and uses the original event/recipient plan. After commit queue one link-only message per eligible identity and version; current authorization is rechecked by REL-01.

```mermaid
sequenceDiagram
    autonumber
    participant W as Background Worker
    participant D as PostgreSQL
    participant N as Email Provider
    W->>D: Claim committed announcement.published.v1 delivery
    D-->>W: Tenant, resource version and intended recipient
    W->>D: Revalidate tenant and recipient scope under REL-01 exceptions
    alt Recipient no longer eligible or resource unavailable
        W->>D: Mark Suppressed with reason, COMMIT
    else Still eligible
        W->>D: Record stable attempt reference, COMMIT
        W->>N: Send approved link-only message for this resource
        alt Provider accepts
            N-->>W: Accepted message reference
            W->>D: Record Accepted, COMMIT
        else Failure or uncertain timeout
            W->>D: Record RetryDue, Failed or Unknown, COMMIT
        end
    end
    Note over W,N: No database transaction spans email, unknown acceptance needs reconciliation
```
