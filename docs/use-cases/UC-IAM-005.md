# UC-IAM-005 — Update profile and notification preferences

[Catalogue](../use-case-catalog.md) · [Architecture](../solution-design.md) · [Shared contracts](../shared-patterns.md)

## 1. Identity and references

**ID:** UC-IAM-005. **Module:** Identity. **Release:** MVP2. **Requirement references:** BR-01, BR-04. **API family:** API-IAM-005. **Screen:** S-04. **Status:** proposed implementation contract; business rules require the listed stakeholder validation.

## 2. Business objective and user story

As a user, I want to keep contact details and optional notification settings current, so the organization can complete this goal with a traceable result. This release implements the stated goal only; future module dependencies are not implied.

## 3. Actors

**Primary:** User. **Supporting:** Frontend and Backend API; PostgreSQL persists or retrieves the authorized business state.  

## 4. Trigger and preconditions

**Trigger:** The primary actor initiates the named action from S-04. Dependencies/capabilities: [UC-IAM-004](UC-IAM-004.md). Required referenced records must exist in the authorized scope; the target state must permit this action. Ordinary tenant activity requires Active tenant and effective membership; authorized export/offboarding exceptions follow the narrow operational procedures.

## 5. Authorization

Own global profile; tenant preferences for own membership only. Administrators cannot change another identity login address. Apply **AUTH-01**, effective dates and server-side resource checks from [shared-patterns.md](../shared-patterns.md). UI visibility is a convenience only. Any cached/delayed action is reauthorized before use.

## 6. Inputs, validation and business rules

**Inputs:** Display name up to 120 characters, locale, optional contact phone, tenant notification preferences and ETag.

**Rules:** Login email changes go through IdP verified-email workflow and must not retarget existing invitations automatically. Contact phone does not become an authentication factor. Security and invitation messages are mandatory; optional issue/comment digests may be disabled.

Text is treated as data; enforce lengths and allowlists server-side. IDs are opaque and must resolve through authorized relationships. No client-supplied role, tenant label or object key establishes permission.

## 7. Main success flow

1. **User:** opens S-04 and initiates “Update profile and notification preferences” with the inputs above.
2. **Frontend:** collects only relevant fields, validates shape, shows the active tenant and submits the listed API operation; protected writes carry session, CSRF, context version and applicable request/ETag values.
3. **Backend:** applies AUTH-01; Own global profile; tenant preferences for own membership only. Administrators cannot change another identity login address.
4. **Backend → Database:** reads Own UserIdentity and MembershipPreference versions through the authorized scope; evaluates the workflow-specific rules in section 6.
5. **Backend:** Save profile and tenant notification preferences. Persistence enforces invariants and records the result and audit; any event is committed through REL-01.
6. **Integration:** None; the committed outcome is visible in the application. No other external provider call is required for the synchronous business result.
7. **Backend → Frontend → User:** Updated profile and effective preferences with new version. Preserve safe user input on a recoverable error and show the returned state/version.

## 8. Alternatives and exceptions

Invalid locale/phone yields field errors. Unverified email remains in IdP pending state. Stale update returns 412; global changes do not copy preferences to other tenants.

ERR-01 applies: malformed input is 400/422; missing authentication is 401; disallowed role is 403; invisible resource is 404. Never reveal another tenant through constraint names, counts or error details. Stale edits require reload/review; identical successful command replay returns its earlier result after fresh authorization. Database failure rolls back the domain write and its outbox; the UI must query the command outcome before resubmitting an uncertain operation.

## 9. Postconditions and failure guarantees

**Success:** Updated profile and effective preferences with new version. **Failure:** rejected authorization or validation does not change domain records. Only committed state is authoritative; audit and outbox do not announce a rolled-back change. External side effects can fail after commit and are reconciled under REL-01.

## 10. Data, APIs and events

**Entities:** `UserIdentity`; `MembershipPreference`; definitions in [data-model.md](../data-model.md). **API operations:** GET /api/v1/me; PATCH /api/v1/me; GET /api/v1/t/{t}/preferences; PUT /api/v1/t/{t}/preferences. Contracts and errors: [API-IAM-005](../api-catalog.md#api-iam-005). **Business event:** `None`. No business event is emitted by this read/authentication operation; security auditing is separate.

## 11. Transaction, consistency and retries

Use CON-01: authorize first, validate current state inside the transaction, apply tenant-aware constraints, and commit the business change with its audit and any outbox records. State updates require If-Match; append/create commands use durable business uniqueness plus a request key. Same-key same-payload replay returns the stored outcome; different payload returns 409. Never retry a state change with a new key after an uncertain response.

## 12. Audit and notifications

Record action, actor/service identity, tenant where applicable, resource ID, safe transition fields, reason reference, timestamp and correlation ID. None; the committed outcome is visible in the application. Never log tokens, raw emails, phone numbers, issue/comment bodies, financial narrative, ballot choices or file bytes. Sensitive business evidence remains in authorized records, not telemetry. Finance audit includes posting IDs and control totals, never editable history.

## 13. Acceptance criteria

- **AT-UC-IAM-005-01 — Outcome:** Given the stated actor, scope and valid inputs, when this workflow succeeds, then updated profile and effective preferences with new version.
- **AT-UC-IAM-005-02 — Business boundary:** Given a user disables optional messages in tenant A, when this workflow is exercised, then mandatory security notifications and tenant B preferences are unchanged.
- **AT-UC-IAM-005-03 — Authorization:** Given the caller lacks the required tenant/building/unit or resource scope, when a known ID is substituted in this workflow, then the API denies access with no protected content, domain mutation or notification.
- **AT-UC-IAM-005-04 — Failure:** Given validation fails or the database transaction aborts, when the client checks the outcome, then no partial domain change or queued external side effect is reported as successful.

The cross-scope test applies both to a second independent tenant and to a denied building/unit within the same tenant where that resource exists. Execution evidence belongs in the release gate; the design itself is not a test result.

## 14. End-to-end sequence

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    U->>F: Update profile and notification preferences
    F->>A: PUT /api/v1/t/{t}/preferences with session and CSRF
    A->>D: Validate own session and membership for tenant preferences
    D-->>A: Own profile scope and active tenant membership
    A->>A: Authorize action and resource scope before domain access
    alt Authentication or scope denied
        A-->>F: 401, 403 or concealed 404, no domain write
        F-->>U: Sign-in or unavailable action
    else Authorized context
        A->>D: BEGIN scoped transaction and acquire required control locks
        A->>D: Scoped read of Own UserIdentity and MembershipPreference versions
        D-->>A: Authorized records and versions
        A->>A: Validate business rules, expected version and command key
        alt Invalid, stale or duplicate conflict
            A->>D: ROLLBACK with no domain side effect
            A-->>F: 422, 412 or 409, refresh or correct input
        else Valid command
            A->>D: Save own tenant notification preferences
            A->>D: Enforce tenant keys and invariants, append audit
            A->>D: COMMIT domain result and command deduplication
            A-->>F: 200 or 201 committed result and current version
        end
        F-->>U: Show returned outcome for update profile and notification preferences or actionable error
    end
```
