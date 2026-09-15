# Sprint 1: concrete acceptance criteria

**Epic #66 — Authentication & User Management.** Sprint 1 contains #62 “Administrator can Authenticate”, #63 “Administrator can create users” and #64 “Registered user can authenticate”. Both authentication stories use the same login implementation. [Internship backlog](internship-backlog.md) assigns tasks and separates Sprint 2 extensions.

September Sprint 1 does not require buildings, units, invitations, MFA or OAuth integration. MFA and OAuth remain phase 2 semester deliverables. [API contract](api-catalog.md) defines exact payloads and statuses; [shared rules](shared-patterns.md) define middleware and transaction behavior.

## Exact validation rules

Apply transformations in this order: reject missing/wrong types, normalize as below, validate length, then format. Missing/null required string gives `This field is required.`; a non-string value or unknown/duplicate property gives 400 INVALID_REQUEST. For strings that fail multiple rules, return the first failure below. Display all invalid fields in the response.

| Field | Rules, in evaluation order | Exact field error |
| --- | --- | --- |
| `email` | Strip only surrounding ASCII space U+0020; lowercase ASCII A–Z. Required after trimming. Length 3–254; ASCII only; local part ≤64; regex below; local part cannot start/end with dot or contain `..`; each domain label ≤63 | Empty: `This field is required.`; all other failures: `Enter a valid email address.` |
| `displayName` | Strip surrounding ASCII space; 1–100 Unicode scalar values; reject U+0000–001F and U+007F–009F. Romanian diacritics, Cyrillic, punctuation and internal spaces allowed | Empty: `This field is required.`; length: `Use between 1 and 100 characters.`; controls: `Remove control characters.` |
| `temporaryPassword`, `newPassword` | **15–128 Unicode scalar values**; no trim, normalization or truncation; reject C0/C1 controls (ranges above) and unpaired surrogates; permit spaces, Unicode letters, digits, punctuation and emoji. No uppercase/digit/symbol composition quota | Empty: `This field is required.`; length: `Use between 15 and 128 characters.`; controls/invalid Unicode: `Remove control characters.` |
| `confirmPassword` | Required; exact ordinal equality to temporary/new password including spaces and Unicode representation | Empty: `This field is required.`; mismatch: `Passwords do not match.` |
| Login `password`, `currentPassword` | Required; 1–128 scalar values; same character restrictions; do not enforce new-password minimum at login, so malformed credentials do not reveal password policy/history | Empty: `This field is required.`; length: `Use between 1 and 128 characters.`; controls: `Remove control characters.` |
| `newPassword` additional | Must differ ordinally from current password, after its format checks | `Choose a different password.` |
| `isActive` | Required JSON boolean, never string/number/null | 400 INVALID_REQUEST (empty `fields`) |
| `page` | Canonical decimal integer 1–10000, default 1; reject `0`, `1.5`, `01`, whitespace, repeats | Field `page`: `Enter a page number from 1 to 10000.` |

Email regex (case-sensitive **full-string match** after lowercase; JS must additionally verify match length because `$` can match before a final newline; .NET use `\A`/`\z` or equivalent full-match check):

```regex
^[a-z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-z0-9](?:[a-z0-9-]*[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)+$
```

This is an intentionally restricted application email format, not a complete RFC mailbox parser or proof of mailbox ownership. Accept `Ana.Popescu+bloc@example.md` (stored `ana.popescu+bloc@example.md`). Reject `a..b@example.md`, `a@localhost`, `a@-bloc.md`, `a b@example.md`, line breaks and internationalized mailbox characters. Email verification/mail delivery are excluded; administrators verify contact details out of band. Global uniqueness is enforced after canonicalization.

Use the same scalar count in JS (`Array.from(value).length` with separate invalid-surrogate rejection) and .NET (`EnumerateRunes` after well-formed UTF-16 validation). Do not rely solely on HTML `maxlength`, which counts UTF-16 units. No password trimming even at paste. For a new password of 14/15/128/129 scalars, expect reject/accept/accept/reject. Use 15 emoji to test client/server agreement. Validation is authoritative on the API.

## #62 — Administrator can Authenticate

**Story:** As an administrator, I want to sign in securely and access my association’s administration functions. Authentication, password-change and sign-out behavior is shared with #64; implement one login flow and test both roles.

| ID | Given / when | Required outcome |
| --- | --- | --- |
| S1-62-01 | Active user, permanent password, valid CSRF; POST valid login | 200 `{"next":"app"}`; new full session/cookie; GET me 200 with only active own memberships; never return password/hash/token in JSON |
| S1-62-02 | Unknown email, wrong password, inactive user or locked account | Exactly 401 `{"error":{"code":"INVALID_CREDENTIALS","message":"Email or password is incorrect.","fields":{}}}`; no session created |
| S1-62-03 | Empty email/password | 400 VALIDATION_FAILED; both fields `This field is required.`; no credential lookup until validation passes |
| S1-62-04 | Fifth consecutive failed password attempt | Lock for 15 minutes; same 401, including correct password during lock; correct password succeeds at expiry; concurrent failure increments preserved |
| S1-62-05 | Eleventh login attempt from same IP within 60s | 429 RATE_LIMITED and Retry-After; after reset another attempt may proceed |
| S1-62-06 | Missing/invalid CSRF on login | 403 CSRF_INVALID, no session or login attempt against account; cookie attributes match shared rules |
| S1-62-07 | Missing, forged, expired or deleted session on GET me | 401 UNAUTHENTICATED; full session works at 29m59s idle but fails at 30m; absolute 8h cannot be extended |
| S1-62-08 | Valid logout with CSRF | 200 `{"message":"Signed out."}`; DB session removed, cookie expired; replay of old cookie yields 401; other sessions unaffected |
| S1-62-09 | User has zero active memberships | Login/me still 200; selector shows no-access message and profile/logout; all association routes return 404 |
| S1-62-10 | Temporary password verified | 200 `{"next":"change_password"}`; only restricted session; GET me and association endpoints 403 PASSWORD_CHANGE_REQUIRED |
| S1-62-11 | Restricted session changes password with correct current password and valid new confirmation | 200 password-change message; stored hash changes; all sessions deleted; old password fails; fresh login with new password returns app |

## #63 — Administrator can create users

**Story:** As an association admin, I want to create resident accounts so approved users can authenticate. Listing and activation/deactivation are delivered in Sprint 2.

| ID | Given / when | Required outcome |
| --- | --- | --- |
| S1-63-01 | Active admin submits valid account creation | 201 membership-user DTO and Location; exactly one global user and one active resident membership committed; password salted+hashed; must-change flag true; never return temporary password |
| S1-63-02 | Duplicate normalized email, including another association's user | 409 EMAIL_UNAVAILABLE, identical body; no new user/membership; concurrent duplicate requests produce one 201 and one 409 |
| S1-63-03 | Email/name/password/confirmation invalid | 400 field errors per rules; no DB writes; forced DB failure between user and membership inserts rolls both back |
| S1-63-04 | POST includes `role`, `roleId`, `associationId`, `id`, `isActive` or any extra property | 400 INVALID_REQUEST after authorization; caller cannot create admin or inject scope |
| S1-63-05 | Anonymous/resident/foreign-association caller | Respectively 401 UNAUTHENTICATED / 403 FORBIDDEN / 404 NOT_FOUND; no change |

Temporary credentials are handed directly to the intended demo user out of band and cleared from UI memory after creation. The creator knows the temporary password until it is changed: demonstrate the forced change and explain this limitation. Do not send email, build invitation tokens or claim verified email in Sprint 1.

## #64 — Registered user can authenticate

**Story:** As a registered user, I want to authenticate, access permitted pages and sign out securely. The identity endpoint returns my profile and memberships; profile editing is a Sprint 2 extension.

| ID | Given / when | Required outcome |
| --- | --- | --- |
| S1-64-01 | Active registered resident authenticates through the shared login endpoint, completes any required password change, then GET me | 200 login result and 200 exact profile DTO with only own active memberships; no separate resident login endpoint |
| S1-64-02 | Login JSON includes role, id, passwordHash, associationId or isActive | 400 INVALID_REQUEST; caller cannot set identity, role or association through login fields |
| S1-64-03 | Resident directly calls any admin user endpoint | 403 FORBIDDEN even if admin controls are hidden in React |
| S1-64-04 | User A substitutes association B or its user ID in routes | 404 for inaccessible association/resource; no existence/details leak; same behavior for random well-formed IDs |
| S1-64-05 | Same user resident in A and admin in B | Resident permissions in A, admin permissions in B; switching UI association cannot carry role from B into A |
| S1-64-06 | Missing/invalid CSRF during logout | 403 CSRF_INVALID; session remains valid until authorized logout or expiry |
| S1-64-07 | Name contains `<script>alert(1)</script>` within length | Stored as text; rendered visibly as literal text, never executes on profile or admin list; no blanket HTML stripping required |
| S1-64-08 | User signs out and replays that session, or presents an expired session | 401 UNAUTHENTICATED; protected UI redirects to login and clears account data |

## Sprint 2 account-management acceptance

These cases belong to the Sprint 2 stories in the [internship backlog](internship-backlog.md#sprint-2-1418-september). They are not extra completion conditions for Sprint 1 #63/#64.

| ID | Given / when | Required outcome |
| --- | --- | --- |
| S2-ADM-01 | Admin GET users, including page beyond end | 200 only own association membership-user DTOs; correct scoped total, max 20 items, empty page allowed; no hash, MFA secret or other memberships |
| S2-ADM-02 | Admin disables resident membership | 200 DTO with isActive false; resident's next association request 404 despite existing session; own profile and other associations remain accessible |
| S2-ADM-03 | Admin enables disabled resident membership | 200 DTO with isActive true; association becomes available again; credentials unchanged; repeating same active state is 200, no duplicate rows |
| S2-ADM-04 | Admin attempts to disable self/another admin or a foreign user | In-association admin target 403 FORBIDDEN; foreign/nonexistent user 404 NOT_FOUND; no credential/global activity changes |
| S2-PRO-01 | Full session reads profile and PATCHes displayName | 200 exact profile DTO; normalized name persists; email is read-only |
| S2-PRO-02 | Profile PATCH includes email, role, id, passwordHash, associationId or isActive | 400 INVALID_REQUEST; no changes |
| S2-PRO-03 | Missing/invalid CSRF during profile mutation | 403 CSRF_INVALID; profile unchanged |
| S2-PRO-04 | User changes password with two active sessions | 200 password-change message; both prior sessions return 401; only new password can authenticate |

## UI acceptance, shared across these stories

- Each input has a visible label and associated error text (`aria-describedby`); invalid inputs set `aria-invalid=true`. Email uses `type=email`, `autocomplete=username`; login password `autocomplete=current-password`; temporary/new/confirmation use `autocomplete=new-password`. Server rules override browser email-validation differences (`noValidate` form plus explicit validation).
- On login page mount focus email; on create-user dialog open focus email; on profile page focus the page heading (`tabIndex=-1`), not an unexpected input. Dialog traps focus; Escape/cancel closes it when idle and restores focus to “Add resident.” No background close while saving.
- Submit starts enabled when idle, including empty forms, so clicking it reveals errors. On invalid submit prevent network request, show all field errors, focus the first invalid field in visible form order. Enter follows identical behavior. Pending submission disables fields, submit and dialog close; button says `Signing in…`, `Creating…` or `Saving…`, and form has `aria-busy=true`. Only one request may be in flight.
- A server 400 attaches field errors and focuses the first invalid field. A 401 login shows the exact generic credential message in a `role=alert` banner, clears password and focuses password. A 409 email conflict keeps non-secret fields, shows exact conflict message by email and focuses email. Other errors show the API message in a focusable alert banner. Network failure uses `Cannot reach the server. Try again.` and re-enables controls; do not automatically repeat account creation.
- All pending states end on success, rejection or network failure. Password show/hide button has an accessible label, defaults hidden, allows paste and password managers; secrets are never logged or stored persistently. Clear secret inputs after success, cancellation or leaving the form. Failed validation retains entered values for correction except the login 401 clearing rule.
- On login success go to association selector (auto-select if exactly one); temporary-password success goes to forced password change. On creation success close the dialog and focus the success notice `Resident created.`. When the Sprint 2 list exists, refresh page 1 and focus the new row if present, otherwise the notice. A 201 is never treated as a 200-only failure. Do not show the submitted password in a toast.
- Resident navigation has Profile, My units/Tickets when phase 2 exists, association switcher and Sign out. Hide Users/Register management. Direct navigation still relies on API authorization; route guard shows `Access denied.` on 403. On 401 clear in-memory profile/association data and navigate to login. On association 404 clear its data and return to selector with `Association unavailable.`. Cancel stale in-flight requests when switching associations and do not display previous-association results under a new heading.
- A 429 disables login submit for Retry-After seconds and shows `Too many requests. Try again later.` plus a visible countdown; fields remain editable. Retry requires explicit submit after countdown.

## Done means demonstrated

All S1 IDs have recorded automated API test results or UI manual test evidence; schema migration succeeds on PostgreSQL 18; both frontend/backend builds pass; one peer reviews each security-relevant change. Record failing tests honestly. Sprint 1 completion requires all S1 cases; the S2 account-management cases are assessed in Sprint 2. No login story is done while role checks, password hashing, CSRF or multi-association tests are missing. Exact HTTP bodies are tested as structured JSON, including absence of sensitive fields.
