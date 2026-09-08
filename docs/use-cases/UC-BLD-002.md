# UC-BLD-002 — Record ownership, move-in, move-out and access dates

[Catalogue](../use-case-catalog.md) · [Architecture](../solution-design.md) · [Shared contracts](../shared-patterns.md)

## 1. Identity and references

**ID:** UC-BLD-002. **Module:** Building register. **Release:** MVP1. **Requirement references:** BR-03, BR-02. **API family:** API-BLD-002. **Screen:** S-08. **Status:** proposed implementation contract; business rules require the listed stakeholder validation.

## 2. Business objective and user story

As a building administrator, I want to maintain truthful unit history and end access at the correct time, so the organization can complete this goal with a traceable result. This release implements the stated goal only; future module dependencies are not implied.

## 3. Actors

**Primary:** Building Administrator. **Supporting:** Frontend and Backend API; PostgreSQL persists or retrieves the authorized business state.  Email Provider is an asynchronous supporting system under REL-01.

## 4. Trigger and preconditions

**Trigger:** The primary actor initiates the named action from S-08. Dependencies/capabilities: [UC-BLD-001](UC-BLD-001.md); [UC-IAM-008](UC-IAM-008.md). Required referenced records must exist in the authorized scope; the target state must permit this action. Ordinary tenant activity requires Active tenant and effective membership; authorized export/offboarding exceptions follow the narrow operational procedures.

## 5. Authorization

Administrator of affected unit; residents cannot edit legal ownership or other persons. Private contact and evidence are restricted to authorized administrators. Apply **AUTH-01**, effective dates and server-side resource checks from [shared-patterns.md](../shared-patterns.md). UI visibility is a convenience only. Any cached/delayed action is reauthorized before use.

## 6. Inputs, validation and business rules

**Inputs:** Person, unit, relationship type, effective start/end, source/reason; separate explicit access-grant start/end action.

**Rules:** Person, OwnershipInterval, OccupancyInterval and UnitAccessGrant are distinct. Co-owners and co-occupants allowed; no duplicate overlapping interval for same person/unit/type. MVP1 owner shares optional descriptive data, not billing rules. Move-out ends the selected occupancy and corresponding occupancy-based grants atomically; ownership/access based on ownership require independent validation.

Text is treated as data; enforce lengths and allowlists server-side. IDs are opaque and must resolve through authorized relationships. No client-supplied role, tenant label or object key establishes permission.

## 7. Main success flow

1. **User:** opens S-08 and initiates “Record ownership, move-in, move-out and access dates” with the inputs above.
2. **Frontend:** collects only relevant fields, validates shape, shows the active tenant and submits the listed API operation; protected writes carry session, CSRF, context version and applicable request/ETag values.
3. **Backend:** applies AUTH-01; Administrator of affected unit; residents cannot edit legal ownership or other persons. Private contact and evidence are restricted to authorized administrators.
4. **Backend → Database:** reads Unit, Person, current relationship intervals and related grants through the authorized scope; evaluates the workflow-specific rules in section 6.
5. **Backend:** Close or add effective intervals and approved unit access grants. Persistence enforces invariants and records the result and audit; any event is committed through REL-01.
6. **Integration:** Send access-end notice after commit; no incoming resident identity is disclosed to the outgoing resident. No other external provider call is required for the synchronous business result.
7. **Backend → Frontend → User:** Accurate historical register; ended grants deny current unit and issue access after their effective end. Preserve safe user input on a recoverable error and show the returned state/version.

## 8. Alternatives and exceptions

Backdated correction requires reason and audit, cannot erase prior activity or silently rewrite MVP3 liability. Transfer does not grant the new owner prior private complaints or former debtor statements. No user is removed from unrelated units.

ERR-01 applies: malformed input is 400/422; missing authentication is 401; disallowed role is 403; invisible resource is 404. Never reveal another tenant through constraint names, counts or error details. Stale edits require reload/review; identical successful command replay returns its earlier result after fresh authorization. Database failure rolls back the domain write and its outbox; the UI must query the command outcome before resubmitting an uncertain operation.

## 9. Postconditions and failure guarantees

**Success:** Accurate historical register; ended grants deny current unit and issue access after their effective end. **Failure:** rejected authorization or validation does not change domain records. Only committed state is authoritative; audit and outbox do not announce a rolled-back change. External side effects can fail after commit and are reconciled under REL-01.

## 10. Data, APIs and events

**Entities:** `Person`; `OwnershipInterval`; `OccupancyInterval`; `UnitAccessGrant`; definitions in [data-model.md](../data-model.md). **API operations:** GET /api/v1/t/{t}/units/{id}/relationships; POST /api/v1/t/{t}/units/{id}/relationship-changes. Contracts and errors: [API-BLD-002](../api-catalog.md#api-bld-002). **Business event:** `unit.relationship_changed.v1`. Envelope, payload whitelist, deduplication and dispatch follow REL-01; the event name does not itself require an external message broker.

## 11. Transaction, consistency and retries

Use CON-01: authorize first, validate current state inside the transaction, apply tenant-aware constraints, and commit the business change with its audit and any outbox records. State updates require If-Match; append/create commands use durable business uniqueness plus a request key. Same-key same-payload replay returns the stored outcome; different payload returns 409. Never retry a state change with a new key after an uncertain response.

## 12. Audit and notifications

Record action, actor/service identity, tenant where applicable, resource ID, safe transition fields, reason reference, timestamp and correlation ID. Send access-end notice after commit; no incoming resident identity is disclosed to the outgoing resident. Never log tokens, raw emails, phone numbers, issue/comment bodies, financial narrative, ballot choices or file bytes. Sensitive business evidence remains in authorized records, not telemetry. Finance audit includes posting IDs and control totals, never editable history.

## 13. Acceptance criteria

- **AT-UC-BLD-002-01 — Outcome:** Given the stated actor, scope and valid inputs, when this workflow succeeds, then accurate historical register; ended grants deny current unit and issue access after their effective end.
- **AT-UC-BLD-002-02 — Business boundary:** Given a resident moves out of one of two units, when this workflow is exercised, then access to that unit ends while the other approved grant remains valid.
- **AT-UC-BLD-002-03 — Authorization:** Given the caller lacks the required tenant/building/unit or resource scope, when a known ID is substituted in this workflow, then the API denies access with no protected content, domain mutation or notification.
- **AT-UC-BLD-002-04 — Failure:** Given validation fails or the database transaction aborts, when the client checks the outcome, then no partial domain change or queued external side effect is reported as successful.

The cross-scope test applies both to a second independent tenant and to a denied building/unit within the same tenant where that resource exists. Execution evidence belongs in the release gate; the design itself is not a test result.

## 14. End-to-end sequence

```mermaid
sequenceDiagram
    autonumber
    actor U as Building Administrator
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    U->>F: Record ownership, move-in, move-out and access dates
    F->>A: POST /api/v1/t/{t}/units/{id}/relationship-changes with session and CSRF
    A->>D: AUTH-01 validate session and active tenant membership
    D-->>A: Role, building grants, effective scope and context version
    A->>A: Authorize action and resource scope before domain access
    alt Authentication or scope denied
        A-->>F: 401, 403 or concealed 404, no domain write
        F-->>U: Sign-in or unavailable action
    else Authorized context
        A->>D: BEGIN scoped transaction and acquire required control locks
        A->>D: Scoped read of Unit, Person, current relationship intervals and related grants
        D-->>A: Authorized records and versions
        A->>A: Validate business rules, expected version and command key
        alt Invalid, stale or duplicate conflict
            A->>D: ROLLBACK with no domain side effect
            A-->>F: 422, 412 or 409, refresh or correct input
        else Valid command
            A->>D: Close or add effective intervals and approved unit access grants
            A->>D: Enforce tenant keys and invariants, append audit and outbox
            A->>D: COMMIT domain result and command deduplication
            A-->>F: 200 or 201 committed result and current version
        end
        F-->>U: Show returned outcome for record ownership, move-in, move-out and access dates or actionable error
    end
    Note over A,D: External delivery happens after commit under REL-01, no atomic provider commit
```

### Notification continuation for UC-BLD-002

The business result above is already committed. This continuation is initiated by the hosted worker and uses the original event/recipient plan. Send access-end notice after commit; no incoming resident identity is disclosed to the outgoing resident.

```mermaid
sequenceDiagram
    autonumber
    participant W as Background Worker
    participant D as PostgreSQL
    participant N as Email Provider
    W->>D: Claim committed unit.relationship_changed.v1 delivery
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

### Move-out with independent access bases

Ending occupancy closes only grants that depend on that occupancy. Separately approved ownership/other-unit grants are evaluated independently; no historical content is erased.

```mermaid
sequenceDiagram
    autonumber
    actor U as Building Administrator
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    U->>F: Review person, unit, move-out date and affected access
    F->>A: POST relationship-changes with session, CSRF and expected version
    A->>D: AUTH-01 validate administrator and building scope
    D-->>A: Current grants and context
    A->>D: BEGIN, lock relationship and affected access rows
    D-->>A: Occupancy plus explicit access-basis references
    A->>A: Validate date and preserve independent ownership/other-unit grants
    alt Valid scoped change
        A->>D: Close occupancy and dependent grants at effective instant
        A->>D: Append reasoned history and access-change outbox, COMMIT
        A-->>F: 200 changed grants and retained history summary
        F-->>U: Show exact end date and remaining valid access
    else Stale interval or invalid relation
        A->>D: ROLLBACK
        A-->>F: 412 or 422, no partial access change
    end
```
