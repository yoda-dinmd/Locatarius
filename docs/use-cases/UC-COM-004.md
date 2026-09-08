# UC-COM-004 — Find and download authorized documents

[Catalogue](../use-case-catalog.md) · [Architecture](../solution-design.md) · [Shared contracts](../shared-patterns.md)

## 1. Identity and references

**ID:** UC-COM-004. **Module:** Communication. **Release:** MVP2. **Requirement references:** BR-04. **API family:** API-COM-004. **Screen:** S-11. **Status:** proposed implementation contract; business rules require the listed stakeholder validation.

## 2. Business objective and user story

As a resident, I want to retrieve a current document without exposing private object URLs, so the organization can complete this goal with a traceable result. This release implements the stated goal only; future module dependencies are not implied.

## 3. Actors

**Primary:** Resident. **Supporting:** Frontend and Backend API; PostgreSQL persists or retrieves the authorized business state. Object Storage and Background Worker support private file handling. 

## 4. Trigger and preconditions

**Trigger:** The primary actor initiates the named action from S-11. Dependencies/capabilities: [UC-COM-003](UC-COM-003.md). Required referenced records must exist in the authorized scope; the target state must permit this action. Ordinary tenant activity requires Active tenant and effective membership; authorized export/offboarding exceptions follow the narrow operational procedures.

## 5. Authorization

Active membership and current grant matching document audience; administrator within managed building scope. Document visibility never implies access to every file in its tenant. Apply **AUTH-01**, effective dates and server-side resource checks from [shared-patterns.md](../shared-patterns.md). UI visibility is a convenience only. Any cached/delayed action is reauthorized before use.

## 6. Inputs, validation and business rules

**Inputs:** Document ID/version, category filter and opaque pagination cursor.

**Rules:** Download through same-origin authenticated backend stream with private no-store response, safe content-disposition and content-type. No public or reusable presigned download links. Authorization is rechecked before storage read, including scan state and retirement.

Text is treated as data; enforce lengths and allowlists server-side. IDs are opaque and must resolve through authorized relationships. No client-supplied role, tenant label or object key establishes permission.

## 7. Main success flow

1. **User:** opens S-11 and initiates “Find and download authorized documents” with the inputs above.
2. **Frontend:** collects only relevant fields, validates shape, shows the active tenant and submits the listed API operation; protected writes carry session, CSRF, context version and applicable request/ETag values.
3. **Backend:** applies AUTH-01; Active membership and current grant matching document audience; administrator within managed building scope. Document visibility never implies access to every file in its tenant.
4. **Backend → Database:** reads Visible DocumentVersion, Attachment metadata and current audience grants through the authorized scope; evaluates the workflow-specific rules in section 6.
5. **Backend:** Authorize document and stream immutable clean object. Return only authorized projections; append any required download/export audit.
6. **Integration:** None; the committed outcome is visible in the application. For attachment content, stream through the backend to/from private object storage; scanning is asynchronous.
7. **Backend → Frontend → User:** Authorized file download or current metadata list; download audit records version and actor. Preserve safe user input on a recoverable error and show the returned state/version.

## 8. Alternatives and exceptions

Stale/retired/missing object returns neutral unavailable state; no bucket paths exposed. Partial stream can be retried after fresh authorization. Cross-unit request returns 404.

ERR-01 applies: malformed input is 400/422; missing authentication is 401; disallowed role is 403; invisible resource is 404. Never reveal another tenant through constraint names, counts or error details. Read failure returns a correlation ID and no fabricated partial business result. Concurrency/version conflict is inapplicable to read-only data access; snapshots and fresh authorization govern consistency.

## 9. Postconditions and failure guarantees

**Success:** Authorized file download or current metadata list; download audit records version and actor. **Failure:** rejected authorization or validation does not change domain records. An attempted-download audit can remain after a failed stream; it is not proof that delivery completed.

## 10. Data, APIs and events

**Entities:** `DocumentVersion`; `Attachment`; `AuditRecord`; definitions in [data-model.md](../data-model.md). **API operations:** GET /api/v1/t/{t}/documents/{id}; GET /api/v1/t/{t}/documents/{id}/versions/{version}/content. Contracts and errors: [API-COM-004](../api-catalog.md#api-com-004). **Business event:** `None`. No business event is emitted by this read/authentication operation; security auditing is separate.

## 11. Transaction, consistency and retries

Read-only business operation; no idempotency key or optimistic write version is needed. Audit append is independent of a streamed download. All reads still require a transaction-local tenant context. Incomplete streams are not valid deliverables.

## 12. Audit and notifications

Record action, actor/service identity, tenant where applicable, resource ID, safe transition fields, reason reference, timestamp and correlation ID. None; the committed outcome is visible in the application. Never log tokens, raw emails, phone numbers, issue/comment bodies, financial narrative, ballot choices or file bytes. Sensitive business evidence remains in authorized records, not telemetry. Finance audit includes posting IDs and control totals, never editable history.

## 13. Acceptance criteria

- **AT-UC-COM-004-01 — Outcome:** Given the stated actor, scope and valid inputs, when this workflow succeeds, then authorized file download or current metadata list; download audit records version and actor.
- **AT-UC-COM-004-02 — Business boundary:** Given a copied download path is opened after membership removal, when this workflow is exercised, then it returns 401 or 404 and no file bytes.
- **AT-UC-COM-004-03 — Authorization:** Given the caller lacks the required tenant/building/unit or resource scope, when a known ID is substituted in this workflow, then the API denies access with no protected content, domain mutation or notification.
- **AT-UC-COM-004-04 — Failure:** Given the database or content read fails, when the request is retried after fresh authorization, then it returns a valid authorized result or a clear unavailable status, never leaked or fabricated data.

The cross-scope test applies both to a second independent tenant and to a denied building/unit within the same tenant where that resource exists. Execution evidence belongs in the release gate; the design itself is not a test result.

## 14. End-to-end sequence

```mermaid
sequenceDiagram
    autonumber
    actor U as Resident
    participant F as Frontend
    participant A as Backend API
    participant D as PostgreSQL
    participant O as Object Storage
    U->>F: Find and download authorized documents
    F->>A: GET /api/v1/t/{t}/documents/{id}/versions/{version}/content with session
    A->>D: AUTH-01 validate session and active tenant membership
    D-->>A: Role, building grants, effective scope and context version
    A->>A: Authorize action and resource scope before domain access
    alt Authentication or scope denied
        A-->>F: 401, 403 or concealed 404, no domain write
        F-->>U: Sign-in or unavailable action
    else Authorized context
        A->>D: Scoped read of Visible DocumentVersion, Attachment metadata and current audience grants
        D-->>A: Authorized records and versions
        A->>A: Recheck current audience, clean scan state and immutable key
        A->>D: Append download audit with actor and version
        A->>O: Read authorized private immutable object
        alt Object available
            O-->>A: File bytes
            A-->>F: 200 authenticated stream, no-store
        else Missing or inaccessible object
            A-->>F: 503 document unavailable, correlation ID
        end
        F-->>U: Show returned outcome for find and download authorized documents or actionable error
    end
```
