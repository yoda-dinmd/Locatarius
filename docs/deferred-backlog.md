# Deferred backlog

As of 16 September 2026, the former Sprint 2/3 proposals below are retained for later planning, **not committed for the remaining internship**. `S2-*` and `S3-*` are historical planning labels, not OpenProject IDs or scheduled sprints. Hour ranges are old estimates and must be reviewed before scheduling. See the [current internship backlog](internship-backlog.md).

Mandatory acceptance checks for #62–#64 and #73–#90 are not deferred by this list. Schedule these proposals after the internship with owners, real work-package IDs and revised estimates.

## Account management and additional verification

These proposals are deferred until after the internship. Existing task acceptance criteria remain required now; overlapping verification work should reuse #75/#78/#86/#90 rather than create duplicate tasks.

### S2-US1 — Administrator manages resident access

As an association administrator, I want to list residents and enable or disable their access so only current approved users can use our association.

**Acceptance:** S2-ADM-01–04 in [account-management acceptance](sprint-1.md#deferred-account-management-acceptance). Disabling a membership blocks that association on the next request, preserves other memberships and cannot disable an admin through this API.

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
| S2-T4 [BE] Own-profile update and password lifecycle | Add displayName-only PATCH with CSRF and validation. Test own-identity derivation, rejected extra fields, two-session password-change invalidation and unchanged email/role. | #83, #87 | 4–6 |
| S2-T5 [FE] Profile and change-password forms | Read-only email, editable name and reusable password form; exact errors, pending states and forced fresh login after password change. Include keyboard and script-like-name rendering checks. | S2-T4, #88 | 4–6 |

### S2-US3 — Authentication resists abuse and exposes safe errors

As a user, I want my account protected against repeated credential attempts and unauthorized browser actions.

**Acceptance:** the implemented Sprint 1 controls also pass concurrent/boundary/browser tests. This story verifies and fixes existing controls; missing mandatory S1 controls keep the corresponding S1 story open.

| Task | Deliverable and done condition | Dependency | Hours |
| --- | --- | --- | --- |
| S2-T6 [BE/QA] Session and throttle verification | Exercise fifth-attempt lockout, IP limit, expiry boundaries, login/password-change races and credential replay. Fix identified failures and retain regression tests. | #75/#85/#86 | 6–10 |
| S2-T7 [FE/BE] Browser security and redacted logging | Verify delivered CSP/cookie/cache headers, CSRF rejection and literal name rendering. Add/verify redacted security events; inspect logs to ensure credentials and bodies are absent. | Integrated API/UI | 4–6 |
| S2-T8 [QA] Two-association end-to-end regression | Exercise admin A/resident A/resident B and dual memberships through browser and direct API calls. Include stale UI requests during association switching. | S2-T1–T7 | 4–6 |


## Delivery, recovery and assessment follow-ups

These additional delivery proposals are unscheduled. Reproducible setup and evidence already required by current tasks remain part of those tasks. Any externally required submission work must be confirmed separately; this list is not a new internship commitment.

### S3-US1 — Team can deploy and recover the MVP

As a team member, I want to start the application from a clean checkout and restore a backup so the demonstration is reproducible and data loss can be handled.

| Task | Deliverable and done condition | Dependency | Hours |
| --- | --- | --- | --- |
| S3-T1 [DEVOPS] Clean setup and CI verification | Build backend/frontend, apply PostgreSQL 18 migrations and run relevant tests in CI. A second student follows the setup instructions successfully on a clean environment. | Integrated account-management extension | 6–8 |
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


