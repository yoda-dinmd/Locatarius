# UC-RES-002 — Reserve or cancel a shared facility

[Catalogue](../use-case-catalog.md) · [Architecture](../solution-design.md) · [Shared contracts](../shared-patterns.md)

## 1. Identity and references

**ID:** UC-RES-002. **Module:** Reservations. **Release:** MVP5. **Requirement references:** BR-09. **API family:** API-RES-002. **Screen:** S-32. **Status:** proposed implementation contract; business rules require the listed stakeholder validation.

## 2. Business objective and user story

As a resident, I want to book an available resource without competing reservations for the same time, so the organization can complete this goal with a traceable result. This release implements the stated goal only; future module dependencies are not implied.

## 3. Actors

**Primary:** Resident. **Supporting:** Frontend and Backend API; PostgreSQL persists or retrieves the authorized business state.  Email Provider is an asynchronous supporting system under REL-01.

## 4. Trigger and preconditions

**Trigger:** The primary actor initiates the named action from S-32. Dependencies/capabilities: [UC-RES-001](UC-RES-001.md). Required referenced records must exist in the authorized scope; the target state must permit this action. Ordinary tenant activity requires Active tenant and effective membership; authorized export/offboarding exceptions follow the narrow operational procedures.

## 5. Authorization

Current resident grant for resource building; creator can cancel own reservation, administrator can cancel within building with reason. Apply **AUTH-01**, effective dates and server-side resource checks from [shared-patterns.md](../shared-patterns.md). UI visibility is a convenience only. Any cached/delayed action is reauthorized before use.

## 6. Inputs, validation and business rules

**Inputs:** Resource ID, start/end instants with displayed local zone, client key; reservation version for cancellation.

**Rules:** Validate opening hours, 90-day horizon, max 2 active future reservations per member, 2-hour maximum baseline and end greater than start. Database exclusion constraint prevents overlap on active reservations; adjacent intervals allowed. Move-out ends future eligibility and marks future reservations Cancelled via dated-access job; request-time checks block use even before cleanup.

Text is treated as data; enforce lengths and allowlists server-side. IDs are opaque and must resolve through authorized relationships. No client-supplied role, tenant label or object key establishes permission.

## 7. Main success flow

1. **User:** opens S-32 and initiates “Reserve or cancel a shared facility” with the inputs above.
2. **Frontend:** collects only relevant fields, validates shape, shows the active tenant and submits the listed API operation; protected writes carry session, CSRF, context version and applicable request/ETag values.
3. **Backend:** applies AUTH-01; Current resident grant for resource building; creator can cancel own reservation, administrator can cancel within building with reason.
4. **Backend → Database:** reads Resource rules, current grants, member quota and conflicting interval through the authorized scope; evaluates the workflow-specific rules in section 6.
5. **Backend:** Create exclusive reservation or record reasoned cancellation. Persistence enforces invariants and records the result and audit; any event is committed through REL-01.
6. **Integration:** After commit queue own booking/cancellation link; public calendar only reveals unavailable times. No other external provider call is required for the synchronous business result.
7. **Backend → Frontend → User:** Confirmed reservation reference or cancelled state with availability released. Preserve safe user input on a recoverable error and show the returned state/version.

## 8. Alternatives and exceptions

Concurrent conflicting booking returns 409 and refreshed availability. Duplicate key returns own existing reservation. Cancellation is idempotent. No email dependency for confirmation.

ERR-01 applies: malformed input is 400/422; missing authentication is 401; disallowed role is 403; invisible resource is 404. Never reveal another tenant through constraint names, counts or error details. Stale edits require reload/review; identical successful command replay returns its earlier result after fresh authorization. Database failure rolls back the domain write and its outbox; the UI must query the command outcome before resubmitting an uncertain operation.

## 9. Postconditions and failure guarantees

**Success:** Confirmed reservation reference or cancelled state with availability released. **Failure:** rejected authorization or validation does not change domain records. Only committed state is authoritative; audit and outbox do not announce a rolled-back change. External side effects can fail after commit and are reconciled under REL-01.

## 10. Data, APIs and events

**Entities:** `Reservation`; `SharedResource`; `ResourceBlock`; definitions in [data-model.md](../data-model.md). **API operations:** GET /api/v1/t/{t}/resources/{id}/availability; POST /api/v1/t/{t}/reservations; GET /api/v1/t/{t}/reservations/me; POST /api/v1/t/{t}/reservations/{id}/cancel. Contracts and errors: [API-RES-002](../api-catalog.md#api-res-002). **Business event:** `reservation.state_changed.v1`. Envelope, payload whitelist, deduplication and dispatch follow REL-01; the event name does not itself require an external message broker.

## 11. Transaction, consistency and retries

Use CON-01: authorize first, validate current state inside the transaction, apply tenant-aware constraints, and commit the business change with its audit and any outbox records. State updates require If-Match; append/create commands use durable business uniqueness plus a request key. Same-key same-payload replay returns the stored outcome; different payload returns 409. Never retry a state change with a new key after an uncertain response.

## 12. Audit and notifications

Record action, actor/service identity, tenant where applicable, resource ID, safe transition fields, reason reference, timestamp and correlation ID. After commit queue own booking/cancellation link; public calendar only reveals unavailable times. Never log tokens, raw emails, phone numbers, issue/comment bodies, financial narrative, ballot choices or file bytes. Sensitive business evidence remains in authorized records, not telemetry. Finance audit includes posting IDs and control totals, never editable history.

## 13. Acceptance criteria

- **AT-UC-RES-002-01 — Outcome:** Given the stated actor, scope and valid inputs, when this workflow succeeds, then confirmed reservation reference or cancelled state with availability released.
- **AT-UC-RES-002-02 — Business boundary:** Given two residents reserve an overlapping interval concurrently, when this workflow is exercised, then exactly one reservation commits and the other sees conflict.
- **AT-UC-RES-002-03 — Authorization:** Given the caller lacks the required tenant/building/unit or resource scope, when a known ID is substituted in this workflow, then the API denies access with no protected content, domain mutation or notification.
- **AT-UC-RES-002-04 — Failure:** Given validation fails or the database transaction aborts, when the client checks the outcome, then no partial domain change or queued external side effect is reported as successful.

The cross-scope test applies both to a second independent tenant and to a denied building/unit within the same tenant where that resource exists. Execution evidence belongs in the release gate; the design itself is not a test result.

## 14. End-to-end sequence

```mermaid
sequenceDiagram
    autonumber
    actor U as Resident
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    U->>F: Reserve or cancel a shared facility
    F->>A: POST /api/v1/t/{t}/reservations with session and CSRF
    A->>D: AUTH-01 validate session and active tenant membership
    D-->>A: Role, building grants, effective scope and context version
    A->>A: Authorize action and resource scope before domain access
    alt Authentication or scope denied
        A-->>F: 401, 403 or concealed 404, no domain write
        F-->>U: Sign-in or unavailable action
    else Authorized context
        A->>D: BEGIN scoped transaction and acquire required control locks
        A->>D: Scoped read of Resource rules, current grants, member quota and conflicting interval
        D-->>A: Authorized records and versions
        A->>A: Validate business rules, expected version and command key
        alt Invalid, stale or duplicate conflict
            A->>D: ROLLBACK with no domain side effect
            A-->>F: 422, 412 or 409, refresh or correct input
        else Valid command
            A->>D: Create exclusive reservation or record reasoned cancellation
            A->>D: Enforce tenant keys and invariants, append audit and outbox
            A->>D: COMMIT domain result and command deduplication
            A-->>F: 200 or 201 committed result and current version
        end
        F-->>U: Show returned outcome for reserve or cancel a shared facility or actionable error
    end
    Note over A,D: External delivery happens after commit under REL-01, no atomic provider commit
```

### Notification continuation for UC-RES-002

The business result above is already committed. This continuation is initiated by the hosted worker and uses the original event/recipient plan. After commit queue own booking/cancellation link; public calendar only reveals unavailable times.

```mermaid
sequenceDiagram
    autonumber
    participant W as Background Worker
    participant D as PostgreSQL
    participant N as Email Provider
    W->>D: Claim committed reservation.state_changed.v1 delivery
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
