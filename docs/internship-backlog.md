# Internship backlog: current commitment

**Planning update — 16 September 2026:** the original Sprint 1 outcomes remain unfinished. The remaining internship, through the currently documented end date of **25 September**, is dedicated to finishing those outcomes. There is no additional Sprint 2/3 feature commitment. Keep weekly sprint tracking, but carry forward unfinished work rather than adding the old extension plan.

## Work-package description review

Use the [latest OpenProject CSV](openproject/README.md) for task descriptions, owners, statuses and dependencies. The 16 September export contains 22 unique work packages: epic #66, stories #62–#64 and tasks #73–#90. It includes #87/#88 first-login work and #89/#90 database follow-ups; do not create duplicate tasks from older planning labels.

The latest export now records this planning update in #66 and the deferred-feature wording in #63/#64/#83. #73 has been reopened and is In progress. Exported status is not proof of tested completion. The former Sprint 2/3 proposals have not been created as work packages.

## Current internship scope

| Work packages | Required outcome |
| --- | --- |
| #62; #73–#78 | Seeded administrator can authenticate through the shared login flow; hashing, environment and frontend integration work |
| #63; #79–#82 | Administrator creates a resident account with validation, safe errors and enforced authorization |
| #64; #83–#88 | Created resident completes first-login password change, loads permitted identity/navigation and signs out securely; authentication and RBAC tests |
| #89 | Database documentation accurately describes the EF Core implementation and records unmet requirements |
| #90 | Fresh-database migrations and admin seeding are verified against PostgreSQL and safe to repeat |

The groupings describe delivery scope; the CSV does not export parent/child relations. Keep the existing OpenProject hierarchy and owners. First-login work is #87/#88 (formerly proposed as S1-A1/A2).

The [internship acceptance criteria](sprint-1.md) retain `S1-*` IDs for traceability. Those IDs identify the original scope, not a claim that it was completed during 7–11 September. Deferred `S2-*` criteria are not additional completion conditions for this internship.

## Contract alignment

The current CSV already contains the revised two-role, shared-authentication task descriptions. The old September 9 review of JWT, role dropdowns and hashing alternatives is no longer the current task specification.

There is still an implementation gap: #73 is now In progress, and the merged EF Core model lacks the associations, memberships and sessions described in its task and referenced by current authentication/authorization requirements. #89 must describe the actual schema and the gap; #90 verifies initialization and admin seeding, not the entire missing authorization model. Reconcile the remaining #73 requirements with its owner and track them explicitly before treating dependent stories as complete. Do not silently remove association isolation or other acceptance requirements to match the current code.

Before integration, agree routes, DTO fields, authentication/session behavior and errors across #75/#77/#79/#80/#83. Record implementation-affecting decisions in [decisions](decisions.md) and align the API, frontend and tests together.

## Work order for the remaining internship

1. Finish the foundation and resolve schema/contract gaps: #73/#74/#78/#89/#90. Keep actual merged work and test evidence; do not restart completed implementation.
2. Integrate shared login, API client and identity/policies: #75/#77/#83. UI work #76/#81 and test design #86 can proceed against agreed contracts.
3. Complete user creation and onboarding: #79–#82 plus #87/#88; connect protected navigation and sign-out #84/#85.
4. Run #86 and each task's required checks, fix defects and record evidence. Demonstrate admin login → resident creation → forced password change → resident login → rejected unauthorized access → logout/replay rejection.

This is dependency guidance, not a new set of work packages or a serial assignment of the whole team. Required testing, HTTPS/environment checks and reproducible setup remain within the existing tasks; they are not postponed to a separate “security sprint.”

## Deferred work

Account lists/details, enable/disable management screens and endpoints, own-profile editing, and the former S2/S3 proposals are retained in the [deferred backlog](deferred-backlog.md). They remain planned work, with dates and capacity to be decided after the internship. Buildings/apartments, tickets/comments and MFA/OIDC remain semester work in the [roadmap](mvp-roadmap.md).

Deferring access-management features does not waive the current stories' authorization checks. Deferring profile editing does not defer first-login password change. Additional recovery drills, release automation and reporting proposals do not replace the evidence required to complete current tasks.

## Keep OpenProject consistent

The latest CSV confirms the scope update to #66, deferral wording in #63/#64/#83 and reopening of #73. No further repetition of those edits is needed.

- Preserve task IDs, owners, time entries, PR links and actual statuses. Update remaining estimates and sprint assignments as the team plans each week.
- Sprint numbers describe scheduling periods; a later Sprint 2 can carry the same unfinished authentication tasks without adopting the old Sprint 2 feature proposals.
- Keep the [deferred proposals](deferred-backlog.md) in documentation until the team schedules them and creates actual work packages.
- Finish the remaining #73 requirements and record evidence before closing it; coordinate the evolving model with #89/#90.
- Add a new CSV snapshot after future OpenProject changes and update the [snapshot guide](openproject/README.md). Never edit a historical export to simulate a live update.

The CSV does not include sprint assignments, dates, activation or board information. It cannot establish which tasks are in the active sprint; verify those in OpenProject rather than inferring them from titles or status.
