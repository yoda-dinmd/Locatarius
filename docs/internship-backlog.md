# Internship backlog: Sprints 1–3

**Epic #66: Authentication & User Management.** The internship delivers secure authentication and basic account management by 25 September. Buildings, unit assignments, tickets and MFA/OIDC integration belong to the October–December phase.

The existing stories and task IDs remain in place. Each task needs an owner, a short description, observable completion criteria and a link to its pull request/test evidence. Preserve assignments, time entries and work in progress when refining descriptions. New references such as `S2-US1` are planning labels, not allocated OpenProject IDs.

## Work-package description review

Source: the OpenProject XLS export dated 9 September 2026. It contains **18 unique work packages**: epic #66, stories #62–#64 and fourteen tasks #73–#86. Its 34 data rows include repeated work packages for relations. The primary work-package Description column contains all three story descriptions and all fourteen task descriptions; related-item description cells are blank. Epic #66’s exported description is blank. All exported statuses are New and assignee cells are blank; these fields do not establish whether implementation has started.

The story outcomes fit the internship. The required changes concern the contracts used by their child tasks. Preserve the hierarchy and existing IDs.

| Work packages | Exported instruction | Proposed description change |
| --- | --- | --- |
| #73, #81, #83 | Roles Admin, Owner and Tenant; #81 permits selecting Admin | Use permission roles `admin` and `resident`. Remove the role dropdown from account creation; the API always creates a resident membership. Occupancy and ownership do not create permission roles. Existing admin provisioning remains an operator task. Reject forged role fields on the server. |
| #73 | User/Role models and default admin seeded at startup | Include association memberships and the selected authentication-state storage. Provide two association fixtures. Seed idempotently from protected configuration; never embed a shared default password in source or reset it at every startup. |
| #74 | BCrypt **or** Argon2id | Select one maintained hasher and explicit parameters. The documented baseline is the framework PBKDF2 hasher. Review an already implemented alternative before replacing it; update password-length/Unicode tests and storage contract together if retained. An algorithm change is a design decision, not a wording correction. |
| #75, #77, #85, #86 | Signed JWT, bearer header, frontend token clearing and JWT-returning tests | Resolve authentication as one coordinated change across issuance, API client, validation, logout and tests. The documentation's cookie-session approach differs from these explicit instructions; implementation progress must be checked before selecting a path. |
| #79 | Global Admin role guard on user creation | Require active admin membership in the selected association. A role attribute alone does not prove association authority. Hash credentials and commit user plus resident membership atomically. |
| #80, #82 | 409 Problem Details, with loosely specified validation/error handling | Use one response format across API, forms and tests. The documented format is `{error:{code,message,fields}}`, not Problem Details. State exact fields, limits and messages; if retaining Problem Details, revise the shared API/UI contract before integration. |
| #81 | First Name, Last Name, Email, Temporary Password and role dropdown | Use `displayName`, `email`, `temporaryPassword`, `confirmPassword` for the documented DTO. No role selection. Coordinate this change with #79/#80/#82; avoid incompatible backend and frontend field names. |
| #75, #79, #82, #83 | `/api/v1/auth/login`, `/api/v1/users`, `/api/v1/me` | Agree one set of paths. `/api/v1` itself is acceptable; association authorization must be explicit whichever routing convention is chosen. |
| #76 | React/Next.js, Tailwind, basic validation and a 401 banner | Specify React as the target, retain Tailwind if already chosen and clarify focus, pending, network and field-error behavior. Next.js is not an additional requirement. |
| #84 | Component name is absent in exported text; redirect to a dashboard | Check the original rich-text field because angle-bracket markup may have been lost during export. Specify the route guard and actual association landing page. No dashboard module is required. API denial remains authoritative. |
| #85 | Clear tokens and frontend identity | Define backend invalidation and credential-replay behavior as well as frontend cleanup. Browser cleanup alone does not meet the documented logout requirement. |
| #86 | Four tests: JWT login, wrong password, resident denial and duplicate email | Keep those scenarios with the agreed authentication assertion. Add inactive-account, anonymous-access, association isolation, expiry/logout replay, safe-response and validation tests. |

**Additional planned security work:** first-login password change, immediate logout revocation, association isolation, rate limits and detailed browser defenses are requirements of the application contract. The exported stories do not spell out all of them. Assign their implementation and tests explicitly rather than treating them as work already described in the export. First-login tasks S1-A1/A2 below are planning proposals, not existing exported tasks.

### Proposed epic and story descriptions

These concise descriptions retain each story's user outcome and can be copied into OpenProject after the team agrees the technical contract. Detailed acceptance rules remain in [Sprint 1](sprint-1.md).

**#66 — Authentication & User Management**

Deliver secure administrator and registered-user authentication, administrator-created resident accounts and association-scoped authorization. By the internship review, demonstrate the admin→created-user flow, rejected unauthorized requests and secure sign-out. Sprint 2 extends account management with lists, membership activation/deactivation and own-profile editing; Sprint 3 provides test, deployment, recovery and assessment evidence. Buildings/tickets and MFA/OAuth are scheduled for the semester phase. Do not add Owner, Tenant or Student permission roles.

**#62 — Administrator can Authenticate**

As an administrator, I want to sign in using my provisioned account so I can access administration functions for my association.

- An active administrator account and association membership exist; its password is stored only as a salted password hash.
- Valid credentials authenticate the administrator through the shared login endpoint; the system derives the correct association role.
- Incorrect credentials, nonexistent accounts and globally inactive accounts receive the same generic 401 response.
- Anonymous callers cannot use protected endpoints; authenticated callers cannot access another association without membership.
- Authentication responses expose no password, hash or unrelated profile data. Enforce the agreed expiry, abuse limits and browser protection rules.
- Evidence: S1-62 tests, implementation tasks #73–#78 and the shared #83/#86 policy/test work.

**#63 — Administrator can create users**

As an association administrator, I want to create resident accounts so approved people can access our association.

- Only a caller with active admin membership in the target association can create an account; resident callers receive 403 and inaccessible associations receive 404.
- Validate `displayName`, `email`, `temporaryPassword` and `confirmPassword` against the shared rules. Do not accept a role field.
- Canonical email is globally unique. Duplicate email returns 409 with the agreed safe error body and no partial records.
- Store only a salted password hash; atomically create the user and active resident membership.
- Success returns 201 with a safe user DTO and confirmation in the interface. Temporary credentials are handled privately and are never logged or returned in a toast.
- Under the documented onboarding contract, a new account must change its temporary password before business access. Link the first-login implementation under #64.
- Evidence: S1-63-01–05 and tasks #79–#82. User listing/access-state controls belong to Sprint 2.

**#64 — Registered user can authenticate**

As a registered user, I want to sign in with my administrator-created account so I can securely access the functions permitted to me.

- Use the same login endpoint as administrators. Valid credentials advance through any required first-login password change and then allow identity loading.
- Incorrect credentials, nonexistent accounts and globally inactive accounts receive generic 401 responses.
- A globally active account may authenticate with zero active association memberships, but receives no association access. Disabling one membership denies that association without disabling unrelated memberships.
- GET identity returns only the caller's profile and active memberships. Server policies reject resident calls to admin functions and reject cross-association access.
- Valid authentication persists across protected navigation and page reload within the agreed expiry. Logout/expiry prevents credential reuse and clears protected frontend state.
- Display field/credential/network errors and prevent duplicate submissions according to the UI criteria.
- Evidence: S1-64-01–08, shared S1-62 authentication/password criteria, tasks #83–#86 and first-login tasks if not already implemented. Profile editing is a separate Sprint 2 outcome.

## Sprint 1: 7–11 September

| Existing story | User outcome | Existing tasks |
| --- | --- | --- |
| #62 Administrator can Authenticate | A seeded administrator signs in and accesses permitted administration functions | #73–#78 |
| #63 Administrator can create users | An authenticated administrator creates a resident account in their association | #79–#82 |
| #64 Registered user can authenticate | A created user signs in through the same login flow, receives their own identity/permissions and signs out | #83–#86 |

One authentication implementation serves both #62 and #64. Different roles require different authorization tests, not separate login endpoints. #64 includes the created-user login scenario even though its existing child tasks emphasize identity, navigation and sign-out. Make #64 dependent on #75 and #63; do not duplicate their implementation tasks.

### Contract alignment

Before integrating the authentication and user-creation tasks, the backend and frontend owners agree authentication, roles, field names, routes and error JSON. Hashing parameters must also be settled with #74’s owner. Independent UI layout and test planning can continue while these contracts are aligned.

| Work package | Exported task specifies | Documented API contract |
| --- | --- | --- |
| #75 | JWT issuance; `POST /api/v1/auth/login` | Server-side session in a secure cookie; `POST /api/auth/login` |
| #79 | `POST /api/v1/users` | `POST /api/associations/{associationId}/users` |
| #83 | `GET /api/v1/me` | `GET /api/auth/me` |

**Recommended baseline:** use the documented server-side cookie session if authentication transport is not implemented. If JWT work is underway, review its code and the cost of aligning either implementation or documentation before changing #75. The export establishes the assigned JWT design but does not establish implementation status. Do not discard working code solely to match endpoint spelling.

A version prefix such as `/api/v1` is a naming decision and can be retained through a coordinated contract update. Association scoping, identity derivation and server-side authorization are functional requirements: a user-creation route must verify the caller's membership in the selected association. Never trust an arbitrary association ID without that check.

If JWT is retained, explicitly define token validation, browser storage/transport, expiry, logout revocation and membership revocation, and update the schema/API/shared rules and tests together. Deleting a browser token alone does not meet the documented immediate-logout replay requirement. JWT issuance alone does not demonstrate OAuth 2.0. No second authentication implementation or refresh-token system is required merely to resolve task wording.

The descriptions below propose alignment with the API catalogue. Changes to the exported JWT/bearer, hashing, role and DTO instructions require coordination with their owners before implementation or merge. These proposals do not establish that OpenProject records or application code have changed.

### Task description updates

Frontend owners can use the [screen mockups](frontend-mockups.md): S-01 for #76/#77, S-02 for first-login password change, S-03 for association selection, and S-06 for #81/#82.

Retain all existing IDs. Task titles can stay where they describe the work accurately; resolve the route/authentication titles above before integration. The following text can be used as the description of each task, with the owning student's name and PR link added.

| Task | Description and completion criteria | Dependency |
| --- | --- | --- |
| #73 — User schema, migrations and initial admin seeding | Implement users, associations, roles, memberships and sessions according to the schema. Apply migrations to an empty PostgreSQL 18 database. Seed two synthetic associations with distinct admin/resident memberships, including a dual-membership test account. Seed commands must be repeatable without duplicates or password resets; read initial secrets from protected input. Record migration/seed commands and verify unique normalized email. | Contract alignment for session storage; hashing #74 |
| #74 — Password hashing service and unit tests | Wrap the framework password hasher, configure the specified work factor and test correct/incorrect passwords, distinct salts for equal passwords and rehash handling. Use the exact creation-password rules. Never log plaintext or implement a cryptographic algorithm. Record actual hash format and test output. | Hasher/parameter agreement; can run alongside #73 |
| #75 — Login endpoint and authenticated session | Implement the agreed login route for both roles. Validate input, return the exact safe JSON errors, enforce failure limits and create/expire/revoke authentication state as specified. With the cookie baseline, include CSRF issuance/validation, secure cookie attributes and restricted temporary-password sessions. Test seeded-admin and created-resident login. Record the agreed mechanism in the description before merge. | #73, #74; contract agreement with #77 |
| #76 — Mobile-first login screen (S-01) | Build one login form for both roles with labels, email/password autocomplete, paste support, first-invalid-input focus and pending/disabled states. Show exact generic credential and network errors. Verify keyboard operation at 360px width. Do not branch into separate admin/resident login screens. | Can start with agreed DTO mocks; integrate with #75/#77 |
| #77 — AuthContext and API client | Centralize API base URL, authenticated request handling, CSRF header handling for cookie mutations, identity loading, 401 cleanup and association selection. Never trust React role checks as authorization. With the cookie baseline, do not store tokens in localStorage/sessionStorage. Refreshing the page restores identity from the server. | Contract agreement; #75, #83 for integration |
| #78 — PostgreSQL and environment verification | Verify PostgreSQL 18, .NET 10 configuration, HTTPS development and protected environment settings. Use a separate limited runtime DB identity and migration identity. Make startup/migration instructions work from a clean checkout; no real secrets in tracked files. Treat an existing older PostgreSQL data volume as a migration, not an in-place image swap. | #73; integration with API host |
| #79 — Admin-guarded user creation | Implement the agreed association-scoped creation route. Verify active admin membership, derive role as resident, and insert user plus membership atomically. Return 201, safe DTO and Location. Reject resident callers with 403 and inaccessible associations with 404. Never allow role or credential changes to an existing global user through creation. | #73, #75, #83; validation #80 |
| #80 — Validation and duplicate conflicts | Apply canonical email, Unicode/name/password and confirmation rules. Return exact 400 field errors and generic 409 EMAIL_UNAVAILABLE for duplicates across any association. Test concurrent duplicate requests and rollback when membership insert fails. No partial account creation. | #79; can develop validators first |
| #81 — Add user/resident form | Implement fields `displayName`, `email`, `temporaryPassword`, `confirmPassword` with no role dropdown, accessible focus/errors and one in-flight submission. Clear credentials after success/cancel. No building picker or user-list dependency in Sprint 1. | Can start with DTO mocks |
| #82 — Connect creation form to API | Submit through shared client and map 201/400/401/403/404/409/network outcomes to the specified form/banner states. Announce Resident created; never display or persist the submitted password in a toast. Verify creating an account and then authenticating as that account. | #79–#81, #77; first-login tasks below |
| #83 — Role policies and identity endpoint | Implement reusable active-membership/admin policies and read-only identity response. Return only caller profile and active memberships; enforce scope/role on the server, including direct requests. Test resident→admin denial, A→B denial and dual-membership role differences. Profile editing belongs to Sprint 2. | #73, #75 |
| #84 — Protected routes and role navigation | Load server identity, show only permitted navigation and prevent protected content flashes while identity loads. Handle 401/403/association 404 consistently and discard stale responses when switching association. Direct URLs must still be denied by API policies. | #77, #83 |
| #85 — Sign-out and session teardown | Connect sign-out to a server endpoint that invalidates the current authentication state, then clear frontend identity/data. In the cookie baseline validate CSRF and expire cookie. A captured pre-logout credential must fail on the next protected request; other sessions remain unaffected. Repeat logout returns the documented 401. Include the backend endpoint in this task or an explicitly linked backend subtask. | #75, #77 |
| #86 — Auth and RBAC integration suite | Test admin login, created-user login, failed credentials, expired/revoked authentication, unauthorized creation, unknown DTO fields and cross-association access. Assert exact status/error shape and absence of secrets. Include concurrent duplicate-account and lockout cases from #80/#75. Run against PostgreSQL 18. | Test design starts immediately; execution follows API integration |

### Missing first-login work

The account-creation contract uses temporary passwords and requires a password change before business access. This work must have an owner; it cannot be left implicit in a login form task.

| Planning label / parent | Task | Completion criteria | Initial estimate |
| --- | --- | --- | --- |
| S1-A1 / #64 | [BE] First-login password change | Implement POST password for the restricted session, validate current/new/confirmation, replace the hash and invalidate all sessions atomically. Restricted session cannot call business routes. Fresh login succeeds only with the changed password. | 4–8 focused hours |
| S1-A2 / #64 | [FE] First-login password-change screen | Route `next=change_password` to a focused, validated form; submit with CSRF, clear secrets on success and return to login. Include keyboard, pending and exact-error checks. | 4–6 focused hours |

If those capabilities already exist under #75/#76, link their implementation and tests instead of creating duplicate tasks. If they cannot finish in Sprint 1, carry the unfinished #64 story into Sprint 2; do not mark it complete while a newly created user cannot finish secure onboarding. Do not silently remove the password-change restriction to make a demonstration pass.

### Sprint 1 review

Demonstrate: admin signs in → creates resident → resident changes temporary password → resident signs in → resident is denied admin functions → both can sign out. Show two-association isolation and password hashing evidence. The [24 S1 criteria](sprint-1.md) are the completion gate. Keep shared authentication tests in one suite and map them to both stories.

## Sprint 2: 14–18 September

**Goal:** complete any Sprint 1 carryover, then make account management usable and verify revocation/security controls. Do not introduce buildings or tickets this week. New stories below can remain under epic #66.

### S2-US1 — Administrator manages resident access

As an association administrator, I want to list residents and enable or disable their access so only current approved users can use our association.

**Acceptance:** S2-ADM-01–04 in [account-management acceptance](sprint-1.md#sprint-2-account-management-acceptance). Disabling a membership blocks that association on the next request, preserves other memberships and cannot disable an admin through this API.

| Task | Deliverable and done condition | Dependency | Hours |
| --- | --- | --- | --- |
| S2-T1 [BE] Scoped user list and access-state endpoints | Implement list/detail and membership PATCH routes, scoped pagination/total, 401/403/404 handling and safe DTOs. Test repeated activation state and admin-target denial. | #79/#83 | 6–8 |
| S2-T2 [FE] User list and access controls | Paged table, active/inactive state, enable/disable controls with pending/error handling, refreshed row and keyboard focus. Render all names as text. | S2-T1; mock first | 6–8 |
| S2-T3 [QA] Membership revocation tests | Test using existing sessions immediately after disabling/re-enabling, foreign targets and dual-membership users. Record results, including a concurrent authorized-write/revocation scenario. | S2-T1 | 4–6 |

### S2-US2 — User manages own profile and password

As an authenticated user, I want to edit my display name and change my password without changing my role or another user's account.

**Acceptance:** S2-PRO-01–04. Email stays read-only; mass-assigned role/identity fields are rejected; password change invalidates all sessions. Reuse Sprint 1 hashing/password-change code.

| Task | Deliverable and done condition | Dependency | Hours |
| --- | --- | --- | --- |
| S2-T4 [BE] Own-profile update and password lifecycle | Add displayName-only PATCH with CSRF and validation. Test own-identity derivation, rejected extra fields, two-session password-change invalidation and unchanged email/role. | #83, S1-A1 | 4–6 |
| S2-T5 [FE] Profile and change-password forms | Read-only email, editable name and reusable password form; exact errors, pending states and forced fresh login after password change. Include keyboard and script-like-name rendering checks. | S2-T4, S1-A2 | 4–6 |

### S2-US3 — Authentication resists abuse and exposes safe errors

As a user, I want my account protected against repeated credential attempts and unauthorized browser actions.

**Acceptance:** the implemented Sprint 1 controls also pass concurrent/boundary/browser tests. This story verifies and fixes existing controls; missing mandatory S1 controls keep the corresponding S1 story open.

| Task | Deliverable and done condition | Dependency | Hours |
| --- | --- | --- | --- |
| S2-T6 [BE/QA] Session and throttle verification | Exercise fifth-attempt lockout, IP limit, expiry boundaries, login/password-change races and credential replay. Fix identified failures and retain regression tests. | #75/#85/#86 | 6–10 |
| S2-T7 [FE/BE] Browser security and redacted logging | Verify delivered CSP/cookie/cache headers, CSRF rejection and literal name rendering. Add/verify redacted security events; inspect logs to ensure credentials and bodies are absent. | Integrated API/UI | 4–6 |
| S2-T8 [QA] Two-association end-to-end regression | Exercise admin A/resident A/resident B and dual memberships through browser and direct API calls. Include stale UI requests during association switching. | S2-T1–T7 | 4–6 |

**Capacity:** 38–56 focused team hours before carryover and review. Reserve at least 25% of available team time for integration/fixes; plan roughly 51–75 hours in total if there is no carryover. These are initial estimates, not student commitments. Reduce new profile polish first when Sprint 1 work remains.

## Sprint 3: 21–25 September

**Goal:** demonstrate a reproducible, tested and explainable secure MVP. No new business modules. Keep release/security tasks in a small internship-delivery epic (new ID) or as project-level work packages if another epic adds little value.

### S3-US1 — Team can deploy and recover the MVP

As a team member, I want to start the application from a clean checkout and restore a backup so the demonstration is reproducible and data loss can be handled.

| Task | Deliverable and done condition | Dependency | Hours |
| --- | --- | --- | --- |
| S3-T1 [DEVOPS] Clean setup and CI verification | Build backend/frontend, apply PostgreSQL 18 migrations and run relevant tests in CI. A second student follows the setup instructions successfully on a clean environment. | Integrated Sprint 2 | 6–8 |
| S3-T2 [DEVOPS/BE] Deployment access and secret review | Verify HTTPS, restricted runtime DB grants, migration credentials, secret injection and safe seeding. Record actual deployed settings and remove unused sample secrets from deployment configuration. | #78, S3-T1 | 4–6 |
| S3-T3 [DEVOPS/QA] Backup restoration rehearsal | Back up synthetic demo data, protect the backup, restore to an isolated PostgreSQL 18 instance, delete restored sessions and verify users/memberships and isolation. Record duration and commands. | S3-T1/T2 | 4–6 |

### S3-US2 — Team can explain and verify the security controls

As a team member, I want evidence of the controls and a response procedure so I can demonstrate how the app handles attacks and incidents.

| Task | Deliverable and done condition | Dependency | Hours |
| --- | --- | --- | --- |
| S3-T4 [QA/ALL] Release security regression and defect closure | Run S1/S2 suites and dependency scans. Fix and retest authentication bypasses, cross-association leaks, plaintext secrets and reproducible XSS; link results to work packages. | S3-T1 and integrated app | 6–8 |
| S3-T5 [SEC/DOC] Threat model and incident rehearsal | Map assets/threats to implemented controls. Rehearse revoking a stolen synthetic session, verify replay fails and record the response timeline. For each control record problem, relevance, implementation, test and limitation. | Working revocation; security logs | 4–6 |

### S3-US3 — Team demonstrates the September outcome

As a team, we want a concise working demonstration and evidence package showing our internship results and remaining semester work.

| Task | Deliverable and done condition | Dependency | Hours |
| --- | --- | --- | --- |
| S3-T6 [DOC/ALL] Internship report and evidence matrix | Link actual PRs/tests/screenshots/restore results, record contributions and known gaps, and distinguish implemented September controls from planned MFA/OAuth and business extensions. | S3-T3–T5 | 4–6 |
| S3-T7 [ALL] Rehearse and tag the reviewed demo | Run the admin→created-user flow, denied admin/cross-association requests and logout replay test. Each student explains a control. Prepare synthetic reset instructions and a reviewed release candidate; tag only after required checks pass. | S3-T4/T6 | 4–6 |

**Capacity:** 32–46 focused team hours before review and contingency, roughly 43–62 hours with a 25% reserve. Unfinished mandatory security work takes priority over presentation polish. MFA/OAuth remain semester requirements, not claimed September achievements.

## Updating work packages already in progress

1. Preserve IDs, owners, time entries and existing implementation links. Confirm the actual in-progress state with each owner; an unchanged “New” status does not establish that no code exists.
2. Edit descriptions to state the user-visible outcome, exact contract, acceptance tests and dependencies. Do not paste the entire architecture into each task.
3. For #75/#79/#83, agree implementation-affecting changes with the owners before updating the shared API/client contract. Record the decision once and link it.
4. Add missing work as explicit subtasks only when it is not already implemented elsewhere. Move incomplete stories to the next sprint with remaining estimates and clear status.
5. Each owner should know what “done” means even if they do not read the whole documentation site. A short task description and shared API/error contract are enough for daily work.

## Exporting descriptions in bulk

Open the project's work-package table, filter to the required stories/tasks and choose **Export** from the table menu. Export the whole filtered set rather than opening individual work packages. Include ID, subject, type, status, assignee, parent and sprint fields where available. Check that filters include children as well as their parent stories. [OpenProject export guide](https://www.openproject.org/docs/user-guide/work-packages/exporting/).

**Preferred:** select XLS (Excel) and enable **Include descriptions**. XLS can also include relations, including parent/child links; that option can repeat a work package across rows. Check the exported row count and a populated Description cell. [XLS options](https://www.openproject.org/docs/user-guide/work-packages/exporting/xls-excel/).

**Alternative:** CSV with **Include descriptions** is suitable for text review; it uses a flat table and may retain raw formatting. [CSV options](https://www.openproject.org/docs/user-guide/work-packages/exporting/csv/). A PDF **report** can include Description through its long-text-field options; a PDF table is intended for tabular overview. [PDF report options](https://www.openproject.org/docs/user-guide/work-packages/exporting/pdf-report/).

Menu labels/options depend on the installed version and permissions. A table export of only the epic does not include every child description automatically; inspect the output. A single XLS/CSV/PDF report containing the selected work packages is sufficient for description review. No API token is needed for this UI export.
