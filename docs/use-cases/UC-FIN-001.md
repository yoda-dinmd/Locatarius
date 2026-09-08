# UC-FIN-001 — Establish debtor accounts and opening balances

[Catalogue](../use-case-catalog.md) · [Architecture](../solution-design.md) · [Shared contracts](../shared-patterns.md)

## 1. Identity and references

**ID:** UC-FIN-001. **Module:** Finance. **Release:** MVP3. **Requirement references:** BR-07. **API family:** API-FIN-001. **Screen:** S-19. **Status:** proposed implementation contract; business rules require the listed stakeholder validation.

## 2. Business objective and user story

As a building administrator, I want to start financial transparency from reconciled liabilities rather than ambiguous unit totals, so the organization can complete this goal with a traceable result. This release implements the stated goal only; future module dependencies are not implied.

## 3. Actors

**Primary:** Building Administrator. **Supporting:** Frontend and Backend API; PostgreSQL persists or retrieves the authorized business state.  

## 4. Trigger and preconditions

**Trigger:** The primary actor initiates the named action from S-19. Dependencies/capabilities: [UC-BLD-002](UC-BLD-002.md). Required referenced records must exist in the authorized scope; the target state must permit this action. Ordinary tenant activity requires Active tenant and effective membership; authorized export/offboarding exceptions follow the narrow operational procedures.

## 5. Authorization

Administrator with finance permission and building scope. Residents see only accounts with explicit AccountAccessGrant; unit access or ownership alone does not grant financial access. Apply **AUTH-01**, effective dates and server-side resource checks from [shared-patterns.md](../shared-patterns.md). UI visibility is a convenience only. Any cached/delayed action is reauthorized before use.

## 6. Inputs, validation and business rules

**Inputs:** Unit, liable party or explicitly agreed joint party, liability interval, currency, cutover date, source reference, signed opening amount in minor units and reviewer acknowledgement.

**Rules:** Account identifies a liability party for a unit during an interval. Transfer creates a new account; old debt remains with previous account unless explicit approved transfer journals are recorded. Positive opening balance is receivable; negative is credit. Opening posting balances against opening-control ledger account.

Text is treated as data; enforce lengths and allowlists server-side. IDs are opaque and must resolve through authorized relationships. No client-supplied role, tenant label or object key establishes permission.

## 7. Main success flow

1. **User:** opens S-19 and initiates “Establish debtor accounts and opening balances” with the inputs above.
2. **Frontend:** collects only relevant fields, validates shape, shows the active tenant and submits the listed API operation; protected writes carry session, CSRF, context version and applicable request/ETag values.
3. **Backend:** applies AUTH-01; Administrator with finance permission and building scope. Residents see only accounts with explicit AccountAccessGrant; unit access or ownership alone does not grant financial access.
4. **Backend → Database:** reads Unit, Person, liability intervals, import source uniqueness and existing journals through the authorized scope; evaluates the workflow-specific rules in section 6.
5. **Backend:** Create account and post reviewed opening journal. Persistence enforces invariants and records the result and audit; any event is committed through REL-01.
6. **Integration:** None; the committed outcome is visible in the application. No other external provider call is required for the synchronous business result.
7. **Backend → Frontend → User:** Open account with immutable opening journal, traceable source and explicit account-access grants. Preserve safe user input on a recoverable error and show the returned state/version.

## 8. Alternatives and exceptions

Unreconciled source blocks posting. Same cutover/source/account cannot post twice. Ambiguous co-owner liability blocks activation until stakeholder decision. No inferred splitting of debt at move-out.

ERR-01 applies: malformed input is 400/422; missing authentication is 401; disallowed role is 403; invisible resource is 404. Never reveal another tenant through constraint names, counts or error details. Stale edits require reload/review; identical successful command replay returns its earlier result after fresh authorization. Database failure rolls back the domain write and its outbox; the UI must query the command outcome before resubmitting an uncertain operation.

## 9. Postconditions and failure guarantees

**Success:** Open account with immutable opening journal, traceable source and explicit account-access grants. **Failure:** rejected authorization or validation does not change domain records. Only committed state is authoritative; audit and outbox do not announce a rolled-back change. External side effects can fail after commit and are reconciled under REL-01.

## 10. Data, APIs and events

**Entities:** `BillingAccount`; `LiabilityParty`; `LiabilityPartyMember`; `AccountAccessGrant`; `Journal`; `JournalLine`; `LedgerAccount`; definitions in [data-model.md](../data-model.md). **API operations:** GET /api/v1/t/{t}/billing-accounts; POST /api/v1/t/{t}/billing-accounts; POST /api/v1/t/{t}/billing-accounts/{id}/opening-balance; POST /api/v1/t/{t}/billing-accounts/{id}/access-grants. Contracts and errors: [API-FIN-001](../api-catalog.md#api-fin-001). **Business event:** `finance.opening_posted.v1`. Envelope, payload whitelist, deduplication and dispatch follow REL-01; the event name does not itself require an external message broker.

## 11. Transaction, consistency and retries

Use CON-01: authorize first, validate current state inside the transaction, apply tenant-aware constraints, and commit the business change with its audit and any outbox records. State updates require If-Match; append/create commands use durable business uniqueness plus a request key. Same-key same-payload replay returns the stored outcome; different payload returns 409. Never retry a state change with a new key after an uncertain response.

## 12. Audit and notifications

Record action, actor/service identity, tenant where applicable, resource ID, safe transition fields, reason reference, timestamp and correlation ID. None; the committed outcome is visible in the application. Never log tokens, raw emails, phone numbers, issue/comment bodies, financial narrative, ballot choices or file bytes. Sensitive business evidence remains in authorized records, not telemetry. Finance audit includes posting IDs and control totals, never editable history.

## 13. Acceptance criteria

- **AT-UC-FIN-001-01 — Outcome:** Given the stated actor, scope and valid inputs, when this workflow succeeds, then open account with immutable opening journal, traceable source and explicit account-access grants.
- **AT-UC-FIN-001-02 — Business boundary:** Given a new owner gains unit access, when this workflow is exercised, then they cannot see the previous owner account or inherit its opening debt.
- **AT-UC-FIN-001-03 — Authorization:** Given the caller lacks the required tenant/building/unit or resource scope, when a known ID is substituted in this workflow, then the API denies access with no protected content, domain mutation or notification.
- **AT-UC-FIN-001-04 — Failure:** Given validation fails or the database transaction aborts, when the client checks the outcome, then no partial domain change or queued external side effect is reported as successful.

The cross-scope test applies both to a second independent tenant and to a denied building/unit within the same tenant where that resource exists. Execution evidence belongs in the release gate; the design itself is not a test result.

## 14. End-to-end sequence

```mermaid
sequenceDiagram
    autonumber
    actor U as Building Administrator
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    U->>F: Establish debtor accounts and opening balances
    F->>A: POST /api/v1/t/{t}/billing-accounts/{id}/opening-balance with session and CSRF
    A->>D: AUTH-01 validate session and active tenant membership
    D-->>A: Role, building grants, effective scope and context version
    A->>A: Authorize action and resource scope before domain access
    alt Authentication or scope denied
        A-->>F: 401, 403 or concealed 404, no domain write
        F-->>U: Sign-in or unavailable action
    else Authorized context
        A->>D: BEGIN scoped transaction and acquire required control locks
        A->>D: Scoped read of Unit, Person, liability intervals, import source uniqueness and existing journals
        D-->>A: Authorized records and versions
        A->>A: Validate business rules, expected version and command key
        alt Invalid, stale or duplicate conflict
            A->>D: ROLLBACK with no domain side effect
            A-->>F: 422, 412 or 409, refresh or correct input
        else Valid command
            A->>D: Post reviewed opening journal to previously created liability account
            A->>D: Enforce tenant keys and invariants, append audit and outbox
            A->>D: COMMIT domain result and command deduplication
            A-->>F: 200 or 201 committed result and current version
        end
        F-->>U: Show returned outcome for establish debtor accounts and opening balances or actionable error
    end
    Note over A,D: External delivery happens after commit under REL-01, no atomic provider commit
```
