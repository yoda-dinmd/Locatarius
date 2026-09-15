# API contract

Base path `/api`. JSON is camelCase. UUIDs are canonical hyphenated strings, timestamps UTC ISO 8601 (`2026-09-09T16:00:00Z`). Routes below are the entire application API scope; initial association/admin setup and recovery are operator commands. [Validation](sprint-1.md) and [authorization order](shared-patterns.md) apply to every route.

## Error envelope

Every API error has exactly these fields; no timestamp, trace ID or framework ProblemDetails additions:

```json
{"error":{"code":"VALIDATION_FAILED","message":"Validation failed.","fields":{"email":["Enter a valid email address."]}}}
```

`fields` is always an object; `{}` outside field validation. Validation returns all invalid fields in form order and one message per field, following the rule order in Sprint 1. JSON object ordering is not significant. The following table defines exact code/message pairs; every non-validation row has `fields:{}`.

| HTTP | code | message |
| --- | --- | --- |
| 400 | `VALIDATION_FAILED` | `Validation failed.` |
| 400 | `INVALID_REQUEST` | `Invalid request.` |
| 401 | `INVALID_CREDENTIALS` | `Email or password is incorrect.` |
| 401 | `UNAUTHENTICATED` | `Authentication required.` |
| 403 | `FORBIDDEN` | `You do not have permission to perform this action.` |
| 403 | `CSRF_INVALID` | `Refresh the page and try again.` |
| 403 | `PASSWORD_CHANGE_REQUIRED` | `Change your password before continuing.` |
| 403 | `MFA_REQUIRED` | `Complete multi-factor authentication.` |
| 404 | `NOT_FOUND` | `Resource not found.` |
| 409 | `EMAIL_UNAVAILABLE` | `This email cannot be used to create an account.` |
| 409 | `UNIT_EXISTS` | `This unit number already exists in this building.` |
| 409 | `ASSIGNMENT_EXISTS` | `This user is already assigned to this unit.` |
| 409 | `TICKET_STATE_CONFLICT` | `The ticket state has changed or does not allow this action.` |
| 413 | `PAYLOAD_TOO_LARGE` | `Request body exceeds 16 KiB.` |
| 415 | `UNSUPPORTED_MEDIA_TYPE` | `Use application/json.` |
| 429 | `RATE_LIMITED` | `Too many requests. Try again later.` |
| 500 | `INTERNAL_ERROR` | `An unexpected error occurred.` |

Successful requests return 200 or 201. Client and authorization errors use 400, 401, 403, 404 or 409. Statuses 413, 415, 429 and 500 cover oversized bodies, unsupported content types, rate limits and server failures. Proxy error handlers must use the same envelope for `/api` requests. 429 includes `Retry-After`. Example denial bodies:

```json
{"error":{"code":"UNAUTHENTICATED","message":"Authentication required.","fields":{}}}
```
```json
{"error":{"code":"FORBIDDEN","message":"You do not have permission to perform this action.","fields":{}}}
```
```json
{"error":{"code":"NOT_FOUND","message":"Resource not found.","fields":{}}}
```
```json
{"error":{"code":"EMAIL_UNAVAILABLE","message":"This email cannot be used to create an account.","fields":{}}}
```

## Phase 1 routes

Sprint 1 delivers login, logout, CSRF, read-only identity, forced password change and account creation. Sprint 2 adds profile editing, account lists/details and activation/deactivation. Both sprints use the contracts below. Task route/authentication alignment is recorded in the [internship backlog](internship-backlog.md#contract-alignment).

`A` abbreviates `/api/associations/{associationId}` in tables only. API URLs must contain the full prefix. No public registration route exists.

| Method and route | Request | Success | Specific failures in addition to shared errors |
| --- | --- | --- | --- |
| GET `/auth/csrf` | None; anonymous allowed | 200 `{"token":"..."}` | 500 |
| POST `/auth/login` | `{"email":"ana@example.com","password":"..."}` | 200 `{"next":"app"}` or `{"next":"change_password"}`; sets session cookie | 400 validation, 401 invalid credentials, 429 |
| POST `/auth/logout` | `{}`; valid session and CSRF required | 200 `{"message":"Signed out."}`; clears session | 401 if already logged out |
| GET `/auth/me` | Full session | 200 profile DTO below | 401 or restricted-session 403 |
| PATCH `/auth/me` | `{"displayName":"Ana Popescu"}` | 200 profile DTO | 400 validation |
| POST `/auth/password` | `{"currentPassword":"...","newPassword":"...","confirmPassword":"..."}` | 200 `{"message":"Password changed. Sign in again."}`; all sessions deleted | 400 validation, 401 invalid credentials |
| GET `A/users?page=1` | Admin | 200 paged membership-user DTOs | 403 resident, 404 association |
| POST `A/users` | Admin; `{"email":"ana@example.com","displayName":"Ana Popescu","temporaryPassword":"...","confirmPassword":"..."}` | 201 membership-user DTO; `Location: /api/associations/{associationId}/users/{userId}` | 400 validation, 409 email unavailable |
| GET `A/users/{userId}` | Admin | 200 membership-user DTO | 404 absent/foreign user |
| PATCH `A/users/{userId}` | Admin; `{"isActive":false}` or true | 200 membership-user DTO | 403 target is admin, 404 absent/foreign user |

Every route in the first column beginning `/auth` is prefixed with `/api`. Password-change restricted sessions can use the password route; MFA restricted sessions cannot. A full session may change its password. Incorrect current password returns 401 INVALID_CREDENTIALS; it does not change stored credentials. Apply the login attempt/lockout policy to current-password verification too, using a separate per-IP 10/minute budget.

Profile DTO is exactly:

```json
{"id":"11111111-1111-4111-8111-111111111111","email":"ana@example.com","displayName":"Ana Popescu","memberships":[{"associationId":"aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa","associationName":"Association A","role":"resident"}]}
```

Only active memberships are included; ordered by association name then ID. Membership-user DTO is exactly `{"id":"<uuid>","email":"ana@example.com","displayName":"Ana Popescu","role":"resident","isActive":true}`. Here `isActive` means **membership** state, not global user state. No API changes global account activity. List includes active and inactive association memberships; ordered by canonical email then ID. Page response: `{"items":[],"page":1,"pageSize":20,"total":0}`; `total` counts only visible records. `page` is an integer 1–10000, defaults to 1; beyond last page returns an empty list. Unknown query parameters return 400 INVALID_REQUEST.

Admin creation always creates `role_id=2`; role/global activity are not accepted input. If canonical email already exists anywhere, return the same 409, do not reveal its association, link it, reset its password or create a membership. A trusted operator can separately attach an existing user to an association after verifying authorization. This small operator task avoids an invitation/linking subsystem.

## Phase 2 register and ticket routes

Full session plus active association membership required. Same response envelope and 20-item pagination apply. There are no generic entity DELETE endpoints.

| Method and route | Access / request | Success |
| --- | --- | --- |
| GET `A/buildings?page=1` | Admin | 200 page of building DTOs |
| POST `A/buildings` | Admin; `{name,address}` | 201 building DTO + Location `A/buildings/{id}` |
| GET `A/buildings/{id}` | Admin | 200 building DTO |
| PATCH `A/buildings/{id}` | Admin; `{name,address}` both required | 200 building DTO |
| GET `A/units?page=1&assignedToMe=true` | Either role: own assigned units when filtered; without filter admin sees all and resident sees assigned only | 200 page of unit DTOs |
| GET `A/units/{id}` | Same visibility | 200 unit DTO |
| POST `A/units` | Admin; `{buildingId,number,floor}` | 201 unit DTO + Location `A/units/{id}`; duplicate 409 UNIT_EXISTS |
| PATCH `A/units/{id}` | Admin; `{number,floor}` | 200 unit DTO; duplicate 409 UNIT_EXISTS |
| GET `A/units/{unitId}/residents?page=1` | Admin | 200 page of membership-user DTOs currently assigned |
| POST `A/units/{unitId}/residents` | Admin; `{userId}`; target globally active user with active membership in association, either role, including caller | 201 `{unitId,userId}`; duplicate 409 ASSIGNMENT_EXISTS |
| DELETE `A/units/{unitId}/residents/{userId}` | Admin; no body | 200 `{"message":"Assignment removed."}`; missing assignment 404 |
| GET `A/tickets?page=1` | Reporter only or admin | 200 page of ticket DTOs |
| POST `A/tickets` | Caller assigned to unit; `{unitId,title,description}` | 201 ticket DTO + Location `A/tickets/{id}` |
| GET `A/tickets/{id}` | Reporter only or admin | 200 ticket DTO |
| PATCH `A/tickets/{id}/status` | Admin; `{expectedStatus,status}` | 200 ticket DTO; invalid/raced transition 409 TICKET_STATE_CONFLICT |
| GET `A/tickets/{id}/comments?page=1` | Same as ticket | 200 page of comment DTOs |
| POST `A/tickets/{id}/comments` | Same as ticket; `{body}` | 201 comment DTO; resolved ticket 409 TICKET_STATE_CONFLICT |

The unit-list query accepts optional `assignedToMe=true` or `assignedToMe=false` (exact lowercase strings, default false). Invalid or repeated values return 400 INVALID_REQUEST. The filter always uses the caller's user ID; it does not accept a target user ID. A resident remains limited to assigned units regardless of this parameter. Both roles use `assignedToMe=true` for My units and the ticket unit picker.

The `/units/{unitId}/residents` routes manage occupants, independently of their permission role. Their lists and assignment picker include active members with either `admin` or `resident` role, including the caller. Only an active association admin may add/remove assignments. Assignment does not change the target's role or grant access to another association. An admin's broad register visibility does not permit ticket creation for an unassigned unit: POST tickets returns 404 in that case.

Building DTO: `{id,name,address}`. Unit DTO: `{id,buildingId,buildingName,buildingAddress,number,floor}`. Ticket DTO: `{id,unitId,reporterUserId,title,description,status,createdAt}`. Comment DTO: `{id,ticketId,authorUserId,body,createdAt}`. IDs/string fields are strings; `floor` is integer or null. No author email/name lookup is exposed to residents. Building lists sort by name then ID; unit lists by building ID, number then ID; tickets newest first then ID; comments oldest first then ID; assigned users by email then ID.

Strings are trimmed using the same normalization as Sprint 1: building name 1–100, address 1–200, unit number 1–20, title 5–120, description 10–2000, comment body 1–2000 Unicode scalar values. Description/comments allow LF line breaks; normalize CRLF/CR to LF before length validation. Other C0/C1 control characters are rejected. Floor is required but nullable; if not null integer −5 through 200. Unit number is case-sensitive (`2A` differs from `2a`). Status is exactly `open`, `in_progress` or `resolved`; only `open→in_progress` and `in_progress→resolved` allowed. Wrong enum uses field message `Select a valid status.`; length message `Use between <min> and <max> characters.`; missing/invalid ID `Enter a valid ID.`; invalid floor `Enter a whole number from -5 to 200, or leave blank.`; disallowed control text `Remove control characters.`. Missing required strings use `This field is required.`; wrong types use INVALID_REQUEST. Foreign/non-visible unit, user, building, ticket or assignment returns 404; an assignment target with an inactive global account or inactive association membership also returns 404. Unknown properties are invalid, including `status` or `reporterUserId` on ticket creation.

## Phase 2 authentication additions

Local login remains available. Successful first-factor verification now returns `{"next":"setup_mfa"}` or `{"next":"verify_mfa"}` with the respective restricted session, after any mandatory password change. Only successful MFA returns `{"next":"app"}` and a rotated full session. Deployment of mandatory MFA deletes all pre-MFA sessions.

| Route | Contract |
| --- | --- |
| POST `/api/auth/mfa/setup` | `mfa_setup` session and `{}`; 200 `{secret,otpauthUri}`; encrypt pending secret, never enable until confirmed |
| POST `/api/auth/mfa/confirm` | `mfa_setup` session and `{code}`; validate pending secret; 200 `{"next":"app"}`, enable MFA and rotate session |
| POST `/api/auth/mfa/verify` | `mfa_challenge` session and `{code}`; 200 `{"next":"app"}` and rotate session |
| GET `/api/auth/oidc/start` | Browser navigation; 302 to configured provider with authorization code + S256 PKCE, state, nonce |
| GET `/api/auth/oidc/callback` | Framework validates response; pre-linked user only; issue appropriate password/MFA restricted session, 302 to fixed local step screen; failure 401 INVALID_CREDENTIALS |

`code` must match `^[0-9]{6}$`; otherwise 400 VALIDATION_FAILED, field `code`: `Enter a 6-digit code.`. Wrong/expired/replayed code returns 401 INVALID_CREDENTIALS. Setup/confirm/verify limited to 5 requests per user per fixed 5-minute window, excess 429 + Retry-After; repeated setup must not bypass limit. Serialize setup/confirmation on the user row. If no seed exists, generate and encrypt one; concurrent/repeated setup returns that same pending seed and never replaces it. If MFA is already enabled, setup/confirm returns 403 MFA_REQUIRED and the client must restart login for a challenge; never reveal an enabled seed. Confirmation and verification lock the user row and rotate/delete the restricted session atomically, so parallel submissions cannot issue multiple full sessions. Use a maintained TOTP library: 30-second steps, 6 digits, HMAC-SHA1 for authenticator compatibility, allow previous/current/next step. Atomically record accepted step in `mfa_last_used_step`; require strictly greater than last accepted step to prevent replay, including across sessions.

OIDC is authentication layered on OAuth 2.0. Configure **one provider**, scopes `openid profile`, exact callback URI, issuer/audience/signature/expiry/state/nonce validation, server-side code exchange, no implicit/password grant. Tokens never reach React and are discarded after validation; subsequent application API access uses its session and local authorization. Pre-link issuer+subject via operator; no public enrollment, email auto-link or provider-role trust. Local MFA is required on both login paths, so external login cannot bypass it. Transient OIDC correlation/nonce cookies are framework-managed, protected, Secure/HttpOnly and short-lived. Their cross-site attributes follow middleware protocol requirements; they are not application sessions. These two browser redirects are the explicit 302 exception to the JSON API contract. Document the demonstrated flow as OAuth/OIDC integration, not as an OAuth authorization server or bearer-token API.
