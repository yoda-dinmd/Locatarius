# UC-TEN-002 — Maintain settings and control tenant suspension

[Catalogue](../use-case-catalog.md) · [Architecture](../solution-design.md) · [Shared contracts](../shared-patterns.md)

## 1. Identity and references

**ID:** UC-TEN-002. **Module:** Tenant lifecycle. **Release:** MVP1. **Requirement references:** BR-02, BR-11. **API family:** API-TEN-002. **Screen:** S-06. **Status:** proposed implementation contract; business rules require the listed stakeholder validation.

## 2. Business objective and user story

As a tenant steward, I want to keep operational settings accurate and stop tenant access when necessary, so the organization can complete this goal with a traceable result. This release implements the stated goal only; future module dependencies are not implied.

## 3. Actors

**Primary:** Tenant steward. **Supporting:** Frontend and Backend API; PostgreSQL persists or retrieves the authorized business state.  

## 4. Trigger and preconditions

**Trigger:** The primary actor initiates the named action from S-06. Dependencies/capabilities: [UC-TEN-001](UC-TEN-001.md). Required referenced records must exist in the authorized scope; the target state must permit this action. Ordinary tenant activity requires Active tenant and effective membership; authorized export/offboarding exceptions follow the narrow operational procedures.

## 5. Authorization

Steward edits settings or requests suspension; only Platform Operator executes suspension/reactivation with verified ticket and MFA. Operator cannot change resident content. Apply **AUTH-01**, effective dates and server-side resource checks from [shared-patterns.md](../shared-patterns.md). UI visibility is a convenience only. Any cached/delayed action is reauthorized before use.

## 6. Inputs, validation and business rules

**Inputs:** Locale, IANA time zone, organization contacts; separate state action with reason, approval reference and ETag.

**Rules:** Active to Suspended denies all routine tenant reads/writes and outbound content notifications. Scoped recovery/export remains available through OP-004. Resume requires incident/contract resolution. MVP3 currency becomes immutable after first posting; zone changes never rewrite stored instants or closed periods.

Text is treated as data; enforce lengths and allowlists server-side. IDs are opaque and must resolve through authorized relationships. No client-supplied role, tenant label or object key establishes permission.

## 7. Main success flow

1. **User:** opens S-06 and initiates “Maintain settings and control tenant suspension” with the inputs above.
2. **Frontend:** collects only relevant fields, validates shape, shows the active tenant and submits the listed API operation; protected writes carry session, CSRF, context version and applicable request/ETag values.
3. **Backend:** applies AUTH-01; Steward edits settings or requests suspension; only Platform Operator executes suspension/reactivation with verified ticket and MFA. Operator cannot change resident content.
4. **Backend → Database:** reads Tenant settings version, approval reference and state through the authorized scope; evaluates the workflow-specific rules in section 6.
5. **Backend:** Save settings or execute approved suspension/reactivation. Persistence enforces invariants and records the result and audit; any event is committed through REL-01.
6. **Integration:** None; the committed outcome is visible in the application. No other external provider call is required for the synchronous business result.
7. **Backend → Frontend → User:** Updated settings or enforced tenant state; suspended UI shows support contact only. Preserve safe user input on a recoverable error and show the returned state/version.

## 8. Alternatives and exceptions

Unauthorized state change returns 403. Suspended jobs pause with explicit status. Stale version returns 412. Failed notification does not undo suspension.

ERR-01 applies: malformed input is 400/422; missing authentication is 401; disallowed role is 403; invisible resource is 404. Never reveal another tenant through constraint names, counts or error details. Stale edits require reload/review; identical successful command replay returns its earlier result after fresh authorization. Database failure rolls back the domain write and its outbox; the UI must query the command outcome before resubmitting an uncertain operation.

## 9. Postconditions and failure guarantees

**Success:** Updated settings or enforced tenant state; suspended UI shows support contact only. **Failure:** rejected authorization or validation does not change domain records. Only committed state is authoritative; audit and outbox do not announce a rolled-back change. External side effects can fail after commit and are reconciled under REL-01.

## 10. Data, APIs and events

**Entities:** `Tenant`; `AuditRecord`; definitions in [data-model.md](../data-model.md). **API operations:** GET /api/v1/t/{t}/settings; PATCH /api/v1/t/{t}/settings; POST /api/v1/platform/tenants/{t}/state. Contracts and errors: [API-TEN-002](../api-catalog.md#api-ten-002). **Business event:** `tenant.state_changed.v1`. Envelope, payload whitelist, deduplication and dispatch follow REL-01; the event name does not itself require an external message broker.

## 11. Transaction, consistency and retries

Use CON-01: authorize first, validate current state inside the transaction, apply tenant-aware constraints, and commit the business change with its audit and any outbox records. State updates require If-Match; append/create commands use durable business uniqueness plus a request key. Same-key same-payload replay returns the stored outcome; different payload returns 409. Never retry a state change with a new key after an uncertain response.

## 12. Audit and notifications

Record action, actor/service identity, tenant where applicable, resource ID, safe transition fields, reason reference, timestamp and correlation ID. None; the committed outcome is visible in the application. Never log tokens, raw emails, phone numbers, issue/comment bodies, financial narrative, ballot choices or file bytes. Sensitive business evidence remains in authorized records, not telemetry. Finance audit includes posting IDs and control totals, never editable history.

## 13. Acceptance criteria

- **AT-UC-TEN-002-01 — Outcome:** Given the stated actor, scope and valid inputs, when this workflow succeeds, then updated settings or enforced tenant state; suspended UI shows support contact only.
- **AT-UC-TEN-002-02 — Business boundary:** Given a tenant is suspended while an email job is queued, when this workflow is exercised, then the worker suppresses content delivery and records the reason.
- **AT-UC-TEN-002-03 — Authorization:** Given the caller lacks the required tenant/building/unit or resource scope, when a known ID is substituted in this workflow, then the API denies access with no protected content, domain mutation or notification.
- **AT-UC-TEN-002-04 — Failure:** Given validation fails or the database transaction aborts, when the client checks the outcome, then no partial domain change or queued external side effect is reported as successful.

The cross-scope test applies both to a second independent tenant and to a denied building/unit within the same tenant where that resource exists. Execution evidence belongs in the release gate; the design itself is not a test result.

## 14. End-to-end sequence

```mermaid
sequenceDiagram
    autonumber
    actor U as Tenant steward
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    U->>F: Maintain settings and control tenant suspension
    F->>A: PATCH /api/v1/t/{t}/settings with session and CSRF
    A->>D: AUTH-01 validate session and active tenant membership
    D-->>A: Role, building grants, effective scope and context version
    A->>A: Authorize action and resource scope before domain access
    alt Authentication or scope denied
        A-->>F: 401, 403 or concealed 404, no domain write
        F-->>U: Sign-in or unavailable action
    else Authorized context
        A->>D: BEGIN scoped transaction and acquire required control locks
        A->>D: Scoped read of Tenant settings version, approval reference and state
        D-->>A: Authorized records and versions
        A->>A: Validate business rules, expected version and command key
        alt Invalid, stale or duplicate conflict
            A->>D: ROLLBACK with no domain side effect
            A-->>F: 422, 412 or 409, refresh or correct input
        else Valid command
            A->>D: Save validated tenant settings
            A->>D: Enforce tenant keys and invariants, append audit and outbox
            A->>D: COMMIT domain result and command deduplication
            A-->>F: 200 or 201 committed result and current version
        end
        F-->>U: Show returned outcome for maintain settings and control tenant suspension or actionable error
    end
    Note over A,D: External delivery happens after commit under REL-01, no atomic provider commit
```

### Operator executes approved suspension

Tenant stewards edit settings; only the restricted operator command executes suspension or reactivation. This diagram shows the materially different actor and permission boundary.

```mermaid
sequenceDiagram
    autonumber
    actor U as Platform Operator
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    U->>F: Execute verified tenant suspension ticket
    F->>A: POST platform tenant state with MFA session, CSRF and If-Match
    A->>D: AUTH-02 validate operator scope, MFA and approval reference
    D-->>A: Approved target tenant and state command
    A->>D: BEGIN, lock target tenant control row and version
    alt Valid Active to Suspended transition
        A->>D: Set Suspended, invalidate contexts, append audit/outbox, COMMIT
        A-->>F: 200 Suspended, routine tenant routes denied
    else Invalid approval or stale state
        A->>D: ROLLBACK
        A-->>F: 403, 409 or 412 with safe reason
    end
    F-->>U: Show tenant service metadata and recorded outcome
```
