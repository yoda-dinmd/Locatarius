# UC-IAM-008 — Assign or replace administrators

[Catalogue](../use-case-catalog.md) · [Architecture](../solution-design.md) · [Shared contracts](../shared-patterns.md)

## 1. Identity and references

**ID:** UC-IAM-008. **Module:** Identity. **Release:** MVP1. **Requirement references:** BR-02, BR-03. **API family:** API-IAM-008. **Screen:** S-05. **Status:** proposed implementation contract; business rules require the listed stakeholder validation.

## 2. Business objective and user story

As a tenant steward, I want to maintain accountable administration without orphaning the organization, so the organization can complete this goal with a traceable result. This release implements the stated goal only; future module dependencies are not implied.

## 3. Actors

**Primary:** Tenant steward. **Supporting:** Frontend and Backend API; PostgreSQL persists or retrieves the authorized business state.  Email Provider is an asynchronous supporting system under REL-01.

## 4. Trigger and preconditions

**Trigger:** The primary actor initiates the named action from S-05. Dependencies/capabilities: [UC-TEN-001](UC-TEN-001.md). Required referenced records must exist in the authorized scope; the target state must permit this action. Ordinary tenant activity requires Active tenant and effective membership; authorized export/offboarding exceptions follow the narrow operational procedures.

## 5. Authorization

Building Administrator with tenant-steward permission and MFA within 15 minutes; Platform Operator emergency recovery follows OP-004 and does not browse tenant records. Apply **AUTH-01**, effective dates and server-side resource checks from [shared-patterns.md](../shared-patterns.md). UI visibility is a convenience only. Any cached/delayed action is reauthorized before use.

## 6. Inputs, validation and business rules

**Inputs:** Candidate verified identity or invitation email, building scope, steward flag, predecessor, reason and expected tenant-control version.

**Rules:** Candidate must accept and satisfy MFA before predecessor loses required coverage. Lock tenant control row when changing protected grants. Keep at least one active steward and one active administrator per active building. Grants can span multiple buildings but never multiple tenants in one command.

Text is treated as data; enforce lengths and allowlists server-side. IDs are opaque and must resolve through authorized relationships. No client-supplied role, tenant label or object key establishes permission.

## 7. Main success flow

1. **User:** opens S-05 and initiates “Assign or replace administrators” with the inputs above.
2. **Frontend:** collects only relevant fields, validates shape, shows the active tenant and submits the listed API operation; protected writes carry session, CSRF, context version and applicable request/ETag values.
3. **Backend:** applies AUTH-01; Building Administrator with tenant-steward permission and MFA within 15 minutes; Platform Operator emergency recovery follows OP-004 and does not browse tenant records.
4. **Backend → Database:** reads Tenant control version and existing/candidate administrator memberships through the authorized scope; evaluates the workflow-specific rules in section 6.
5. **Backend:** Stage administrator invitation or atomically transfer accepted grants. Persistence enforces invariants and records the result and audit; any event is committed through REL-01.
6. **Integration:** After commit notify old and new administrators and remaining steward contacts of scope changed, without resident data. No other external provider call is required for the synchronous business result.
7. **Backend → Frontend → User:** Pending replacement or completed transfer with preserved administrator coverage. Preserve safe user input on a recoverable error and show the returned state/version.

## 8. Alternatives and exceptions

Unaccepted candidate leaves predecessor active. Concurrent last-admin removals allow at most one valid change. Lost administrator recovery requires two-person operator verification and a bounded command.

ERR-01 applies: malformed input is 400/422; missing authentication is 401; disallowed role is 403; invisible resource is 404. Never reveal another tenant through constraint names, counts or error details. Stale edits require reload/review; identical successful command replay returns its earlier result after fresh authorization. Database failure rolls back the domain write and its outbox; the UI must query the command outcome before resubmitting an uncertain operation.

## 9. Postconditions and failure guarantees

**Success:** Pending replacement or completed transfer with preserved administrator coverage. **Failure:** rejected authorization or validation does not change domain records. Only committed state is authoritative; audit and outbox do not announce a rolled-back change. External side effects can fail after commit and are reconciled under REL-01.

## 10. Data, APIs and events

**Entities:** `Invitation`; `Membership`; `BuildingGrant`; `Tenant`; definitions in [data-model.md](../data-model.md). **API operations:** POST /api/v1/t/{t}/administrator-transfers; POST /api/v1/t/{t}/administrator-transfers/{id}/complete. Contracts and errors: [API-IAM-008](../api-catalog.md#api-iam-008). **Business event:** `administrator.transfer_changed.v1`. Envelope, payload whitelist, deduplication and dispatch follow REL-01; the event name does not itself require an external message broker.

## 11. Transaction, consistency and retries

Use CON-01: authorize first, validate current state inside the transaction, apply tenant-aware constraints, and commit the business change with its audit and any outbox records. State updates require If-Match; append/create commands use durable business uniqueness plus a request key. Same-key same-payload replay returns the stored outcome; different payload returns 409. Never retry a state change with a new key after an uncertain response.

## 12. Audit and notifications

Record action, actor/service identity, tenant where applicable, resource ID, safe transition fields, reason reference, timestamp and correlation ID. After commit notify old and new administrators and remaining steward contacts of scope changed, without resident data. Never log tokens, raw emails, phone numbers, issue/comment bodies, financial narrative, ballot choices or file bytes. Sensitive business evidence remains in authorized records, not telemetry. Finance audit includes posting IDs and control totals, never editable history.

## 13. Acceptance criteria

- **AT-UC-IAM-008-01 — Outcome:** Given the stated actor, scope and valid inputs, when this workflow succeeds, then pending replacement or completed transfer with preserved administrator coverage.
- **AT-UC-IAM-008-02 — Business boundary:** Given two stewards concurrently remove each other, when this workflow is exercised, then serialization preserves at least one active steward.
- **AT-UC-IAM-008-03 — Authorization:** Given the caller lacks the required tenant/building/unit or resource scope, when a known ID is substituted in this workflow, then the API denies access with no protected content, domain mutation or notification.
- **AT-UC-IAM-008-04 — Failure:** Given validation fails or the database transaction aborts, when the client checks the outcome, then no partial domain change or queued external side effect is reported as successful.

The cross-scope test applies both to a second independent tenant and to a denied building/unit within the same tenant where that resource exists. Execution evidence belongs in the release gate; the design itself is not a test result.

## 14. End-to-end sequence

```mermaid
sequenceDiagram
    autonumber
    actor U as Tenant steward
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    U->>F: Assign or replace administrators
    F->>A: POST /api/v1/t/{t}/administrator-transfers/{id}/complete with session and CSRF
    A->>D: AUTH-01 validate session and active tenant membership
    D-->>A: Role, building grants, effective scope and context version
    A->>A: Authorize action and resource scope before domain access
    alt Authentication or scope denied
        A-->>F: 401, 403 or concealed 404, no domain write
        F-->>U: Sign-in or unavailable action
    else Authorized context
        A->>D: BEGIN scoped transaction and acquire required control locks
        A->>D: Scoped read of Tenant control version and existing/candidate administrator memberships
        D-->>A: Authorized records and versions
        A->>A: Validate business rules, expected version and command key
        alt Invalid, stale or duplicate conflict
            A->>D: ROLLBACK with no domain side effect
            A-->>F: 422, 412 or 409, refresh or correct input
        else Valid command
            A->>D: Transfer previously accepted MFA-protected administrator grants atomically
            A->>D: Enforce tenant keys and invariants, append audit and outbox
            A->>D: COMMIT domain result and command deduplication
            A-->>F: 200 or 201 committed result and current version
        end
        F-->>U: Show returned outcome for assign or replace administrators or actionable error
    end
    Note over A,D: External delivery happens after commit under REL-01, no atomic provider commit
```

### Notification continuation for UC-IAM-008

The business result above is already committed. This continuation is initiated by the hosted worker and uses the original event/recipient plan. After commit notify old and new administrators and remaining steward contacts of scope changed, without resident data.

```mermaid
sequenceDiagram
    autonumber
    participant W as Background Worker
    participant D as PostgreSQL
    participant N as Email Provider
    W->>D: Claim committed administrator.transfer_changed.v1 delivery
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
