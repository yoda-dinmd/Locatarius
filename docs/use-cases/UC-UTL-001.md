# UC-UTL-001 — Register meters and effective associations

[Catalogue](../use-case-catalog.md) · [Architecture](../solution-design.md) · [Shared contracts](../shared-patterns.md)

## 1. Identity and references

**ID:** UC-UTL-001. **Module:** Utilities. **Release:** MVP4. **Requirement references:** BR-08. **API family:** API-UTL-001. **Screen:** S-27. **Status:** proposed implementation contract; business rules require the listed stakeholder validation.

## 2. Business objective and user story

As a building administrator, I want to attribute readings to the correct unit or common supply over time, so the organization can complete this goal with a traceable result. This release implements the stated goal only; future module dependencies are not implied.

## 3. Actors

**Primary:** Building Administrator. **Supporting:** Frontend and Backend API; PostgreSQL persists or retrieves the authorized business state.  

## 4. Trigger and preconditions

**Trigger:** The primary actor initiates the named action from S-27. Dependencies/capabilities: [UC-BLD-002](UC-BLD-002.md); [UC-MNT-002](UC-MNT-002.md). Required referenced records must exist in the authorized scope; the target state must permit this action. Ordinary tenant activity requires Active tenant and effective membership; authorized export/offboarding exceptions follow the narrow operational procedures.

## 5. Authorization

Administrator of meter building; residents can view only meters currently assigned to their accessible unit. Physical ownership of meter is descriptive and does not grant app access. Apply **AUTH-01**, effective dates and server-side resource checks from [shared-patterns.md](../shared-patterns.md). UI visibility is a convenience only. Any cached/delayed action is reauthorized before use.

## 6. Inputs, validation and business rules

**Inputs:** Meter serial, utility type, measurement unit, precision, installation/retirement dates, unit/common-area association interval, starting value and replacement link.

**Rules:** Serial unique per tenant/utility while active. No overlapping association for one meter. Replacement starts a new meter and baseline reading; never reset a cumulative register silently. Common meter is administrator-only by default.

Text is treated as data; enforce lengths and allowlists server-side. IDs are opaque and must resolve through authorized relationships. No client-supplied role, tenant label or object key establishes permission.

## 7. Main success flow

1. **User:** opens S-27 and initiates “Register meters and effective associations” with the inputs above.
2. **Frontend:** collects only relevant fields, validates shape, shows the active tenant and submits the listed API operation; protected writes carry session, CSRF, context version and applicable request/ETag values.
3. **Backend:** applies AUTH-01; Administrator of meter building; residents can view only meters currently assigned to their accessible unit. Physical ownership of meter is descriptive and does not grant app access.
4. **Backend → Database:** reads Meter, MeterAssociation and effective intervals through the authorized scope; evaluates the workflow-specific rules in section 6.
5. **Backend:** Create meter or close association and register replacement. Persistence enforces invariants and records the result and audit; any event is committed through REL-01.
6. **Integration:** None; the committed outcome is visible in the application. No other external provider call is required for the synchronous business result.
7. **Backend → Frontend → User:** Effective meter history and validated baseline. Preserve safe user input on a recoverable error and show the returned state/version.

## 8. Alternatives and exceptions

Overlap returns 409. Backdated reassociation affecting billed usage requires explicit correction plan, not automatic reallocation. Cross-building association outside admin scope denied.

ERR-01 applies: malformed input is 400/422; missing authentication is 401; disallowed role is 403; invisible resource is 404. Never reveal another tenant through constraint names, counts or error details. Stale edits require reload/review; identical successful command replay returns its earlier result after fresh authorization. Database failure rolls back the domain write and its outbox; the UI must query the command outcome before resubmitting an uncertain operation.

## 9. Postconditions and failure guarantees

**Success:** Effective meter history and validated baseline. **Failure:** rejected authorization or validation does not change domain records. Only committed state is authoritative; audit and outbox do not announce a rolled-back change. External side effects can fail after commit and are reconciled under REL-01.

## 10. Data, APIs and events

**Entities:** `Meter`; `MeterAssociation`; `MeterReading`; definitions in [data-model.md](../data-model.md). **API operations:** GET /api/v1/t/{t}/meters; POST /api/v1/t/{t}/meters; POST /api/v1/t/{t}/meters/{id}/association-changes. Contracts and errors: [API-UTL-001](../api-catalog.md#api-utl-001). **Business event:** `None`. No business event is emitted by this read/authentication operation; security auditing is separate.

## 11. Transaction, consistency and retries

Use CON-01: authorize first, validate current state inside the transaction, apply tenant-aware constraints, and commit the business change with its audit and any outbox records. State updates require If-Match; append/create commands use durable business uniqueness plus a request key. Same-key same-payload replay returns the stored outcome; different payload returns 409. Never retry a state change with a new key after an uncertain response.

## 12. Audit and notifications

Record action, actor/service identity, tenant where applicable, resource ID, safe transition fields, reason reference, timestamp and correlation ID. None; the committed outcome is visible in the application. Never log tokens, raw emails, phone numbers, issue/comment bodies, financial narrative, ballot choices or file bytes. Sensitive business evidence remains in authorized records, not telemetry. Finance audit includes posting IDs and control totals, never editable history.

## 13. Acceptance criteria

- **AT-UC-UTL-001-01 — Outcome:** Given the stated actor, scope and valid inputs, when this workflow succeeds, then effective meter history and validated baseline.
- **AT-UC-UTL-001-02 — Business boundary:** Given a meter is transferred to a new unit, when this workflow is exercised, then historical readings stay with their original effective association.
- **AT-UC-UTL-001-03 — Authorization:** Given the caller lacks the required tenant/building/unit or resource scope, when a known ID is substituted in this workflow, then the API denies access with no protected content, domain mutation or notification.
- **AT-UC-UTL-001-04 — Failure:** Given validation fails or the database transaction aborts, when the client checks the outcome, then no partial domain change or queued external side effect is reported as successful.

The cross-scope test applies both to a second independent tenant and to a denied building/unit within the same tenant where that resource exists. Execution evidence belongs in the release gate; the design itself is not a test result.

## 14. End-to-end sequence

```mermaid
sequenceDiagram
    autonumber
    actor U as Building Administrator
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    U->>F: Register meters and effective associations
    F->>A: POST /api/v1/t/{t}/meters with session and CSRF
    A->>D: AUTH-01 validate session and active tenant membership
    D-->>A: Role, building grants, effective scope and context version
    A->>A: Authorize action and resource scope before domain access
    alt Authentication or scope denied
        A-->>F: 401, 403 or concealed 404, no domain write
        F-->>U: Sign-in or unavailable action
    else Authorized context
        A->>D: BEGIN scoped transaction and acquire required control locks
        A->>D: Scoped read of Meter, MeterAssociation and effective intervals
        D-->>A: Authorized records and versions
        A->>A: Validate business rules, expected version and command key
        alt Invalid, stale or duplicate conflict
            A->>D: ROLLBACK with no domain side effect
            A-->>F: 422, 412 or 409, refresh or correct input
        else Valid command
            A->>D: Create meter or close association and register replacement
            A->>D: Enforce tenant keys and invariants, append audit
            A->>D: COMMIT domain result and command deduplication
            A-->>F: 200 or 201 committed result and current version
        end
        F-->>U: Show returned outcome for register meters and effective associations or actionable error
    end
```
