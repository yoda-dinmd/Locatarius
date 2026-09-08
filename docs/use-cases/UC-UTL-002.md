# UC-UTL-002 — Submit a meter reading

[Catalogue](../use-case-catalog.md) · [Architecture](../solution-design.md) · [Shared contracts](../shared-patterns.md)

## 1. Identity and references

**ID:** UC-UTL-002. **Module:** Utilities. **Release:** MVP4. **Requirement references:** BR-08. **API family:** API-UTL-002. **Screen:** S-28. **Status:** proposed implementation contract; business rules require the listed stakeholder validation.

## 2. Business objective and user story

As a resident, I want to report consumption evidence for an eligible meter and period, so the organization can complete this goal with a traceable result. This release implements the stated goal only; future module dependencies are not implied.

## 3. Actors

**Primary:** Resident. **Supporting:** Frontend and Backend API; PostgreSQL persists or retrieves the authorized business state.  

## 4. Trigger and preconditions

**Trigger:** The primary actor initiates the named action from S-28. Dependencies/capabilities: [UC-UTL-001](UC-UTL-001.md); [UC-ISS-006](UC-ISS-006.md). Required referenced records must exist in the authorized scope; the target state must permit this action. Ordinary tenant activity requires Active tenant and effective membership; authorized export/offboarding exceptions follow the narrow operational procedures.

## 5. Authorization

Resident with current unit grant and meter association valid at reading time; administrator may enter on behalf with attribution. Historical reading scope does not restore ended app access. Apply **AUTH-01**, effective dates and server-side resource checks from [shared-patterns.md](../shared-patterns.md). UI visibility is a convenience only. Any cached/delayed action is reauthorized before use.

## 6. Inputs, validation and business rules

**Inputs:** Meter, reading timestamp, decimal register value as string, optional clean photo, reading-window ID and client key.

**Rules:** Nonnegative precision-bounded cumulative value; reject future time beyond 5 minutes. Reading date must lie in open collection window. Lower register requires explicit replacement/rollover workflow; abnormal increase is flagged for administrator review. Submission is PendingValidation, not automatically billable.

Text is treated as data; enforce lengths and allowlists server-side. IDs are opaque and must resolve through authorized relationships. No client-supplied role, tenant label or object key establishes permission.

## 7. Main success flow

1. **User:** opens S-28 and initiates “Submit a meter reading” with the inputs above.
2. **Frontend:** collects only relevant fields, validates shape, shows the active tenant and submits the listed API operation; protected writes carry session, CSRF, context version and applicable request/ETag values.
3. **Backend:** applies AUTH-01; Resident with current unit grant and meter association valid at reading time; administrator may enter on behalf with attribution. Historical reading scope does not restore ended app access.
4. **Backend → Database:** reads MeterAssociation, window, prior accepted reading and duplicate submission through the authorized scope; evaluates the workflow-specific rules in section 6.
5. **Backend:** Store pending reading and anomaly flags. Persistence enforces invariants and records the result and audit; any event is committed through REL-01.
6. **Integration:** None; the committed outcome is visible in the application. No other external provider call is required for the synchronous business result.
7. **Backend → Frontend → User:** Pending reading reference visible to submitter and scoped administrator. Preserve safe user input on a recoverable error and show the returned state/version.

## 8. Alternatives and exceptions

Duplicate meter/window/value returns previous result; conflicting second reading requires correction proposal. Closed window rejects unless administrator exception with reason. Invalid photo remains unbound.

ERR-01 applies: malformed input is 400/422; missing authentication is 401; disallowed role is 403; invisible resource is 404. Never reveal another tenant through constraint names, counts or error details. Stale edits require reload/review; identical successful command replay returns its earlier result after fresh authorization. Database failure rolls back the domain write and its outbox; the UI must query the command outcome before resubmitting an uncertain operation.

## 9. Postconditions and failure guarantees

**Success:** Pending reading reference visible to submitter and scoped administrator. **Failure:** rejected authorization or validation does not change domain records. Only committed state is authoritative; audit and outbox do not announce a rolled-back change. External side effects can fail after commit and are reconciled under REL-01.

## 10. Data, APIs and events

**Entities:** `MeterReading`; `Meter`; `ReadingWindow`; `Attachment`; definitions in [data-model.md](../data-model.md). **API operations:** GET /api/v1/t/{t}/reading-windows; POST /api/v1/t/{t}/meter-readings; GET /api/v1/t/{t}/meter-readings/{id}. Contracts and errors: [API-UTL-002](../api-catalog.md#api-utl-002). **Business event:** `meter.reading_submitted.v1`. Envelope, payload whitelist, deduplication and dispatch follow REL-01; the event name does not itself require an external message broker.

## 11. Transaction, consistency and retries

Use CON-01: authorize first, validate current state inside the transaction, apply tenant-aware constraints, and commit the business change with its audit and any outbox records. State updates require If-Match; append/create commands use durable business uniqueness plus a request key. Same-key same-payload replay returns the stored outcome; different payload returns 409. Never retry a state change with a new key after an uncertain response.

## 12. Audit and notifications

Record action, actor/service identity, tenant where applicable, resource ID, safe transition fields, reason reference, timestamp and correlation ID. None; the committed outcome is visible in the application. Never log tokens, raw emails, phone numbers, issue/comment bodies, financial narrative, ballot choices or file bytes. Sensitive business evidence remains in authorized records, not telemetry. Finance audit includes posting IDs and control totals, never editable history.

## 13. Acceptance criteria

- **AT-UC-UTL-002-01 — Outcome:** Given the stated actor, scope and valid inputs, when this workflow succeeds, then pending reading reference visible to submitter and scoped administrator.
- **AT-UC-UTL-002-02 — Business boundary:** Given a resident submits a reading for the neighboring unit meter, when this workflow is exercised, then validation returns 404 and creates no reading.
- **AT-UC-UTL-002-03 — Authorization:** Given the caller lacks the required tenant/building/unit or resource scope, when a known ID is substituted in this workflow, then the API denies access with no protected content, domain mutation or notification.
- **AT-UC-UTL-002-04 — Failure:** Given validation fails or the database transaction aborts, when the client checks the outcome, then no partial domain change or queued external side effect is reported as successful.

The cross-scope test applies both to a second independent tenant and to a denied building/unit within the same tenant where that resource exists. Execution evidence belongs in the release gate; the design itself is not a test result.

## 14. End-to-end sequence

```mermaid
sequenceDiagram
    autonumber
    actor U as Resident
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    U->>F: Submit a meter reading
    F->>A: POST /api/v1/t/{t}/meter-readings with session and CSRF
    A->>D: AUTH-01 validate session and active tenant membership
    D-->>A: Role, building grants, effective scope and context version
    A->>A: Authorize action and resource scope before domain access
    alt Authentication or scope denied
        A-->>F: 401, 403 or concealed 404, no domain write
        F-->>U: Sign-in or unavailable action
    else Authorized context
        A->>D: BEGIN scoped transaction and acquire required control locks
        A->>D: Scoped read of MeterAssociation, window, prior accepted reading and duplicate submission
        D-->>A: Authorized records and versions
        A->>A: Validate business rules, expected version and command key
        alt Invalid, stale or duplicate conflict
            A->>D: ROLLBACK with no domain side effect
            A-->>F: 422, 412 or 409, refresh or correct input
        else Valid command
            A->>D: Store pending reading and anomaly flags
            A->>D: Enforce tenant keys and invariants, append audit and outbox
            A->>D: COMMIT domain result and command deduplication
            A-->>F: 200 or 201 committed result and current version
        end
        F-->>U: Show returned outcome for submit a meter reading or actionable error
    end
    Note over A,D: External delivery happens after commit under REL-01, no atomic provider commit
```
