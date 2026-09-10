# Two-phase roadmap: September–December

The project runs through September–December 2026. Five students work during the September internship, ending **25 September**, and continue part-time through the semester. The final submission date is set by the course schedule.

## Phase 1 — September foundation

**Outcome:** #62/#63/#64 work end to end with secure local authentication and basic user management. No building or ticket dependency.

| Window | Deliverable | Evidence / exit |
| --- | --- | --- |
| Internship week 1, through 4 Sep | Agree scope, threat list, repo conventions and screen sketches | Scope signed off in team meeting; five students can explain the data boundary |
| 7–11 Sep, Sprint 1 | Five foundation tables, two test associations, sign-in/out, read-only identity and administrator account creation | S1-62/63/64 acceptance evidence, including password hashing, sessions, CSRF protection and rejected unauthorized requests |
| 14–18 Sep | Finish Sprint 1 carryover, add account lists/access control and own profile editing; integrate and test | Clean builds and reproducible setup; no critical access-control defects |
| 21–25 Sep | Rehearse demo, backup/restore once, document threats and explain implemented protections | Working initial MVP, security analysis, evidence and phase 2 plan |

A story is complete when its acceptance criteria pass and the evidence is recorded. Use the integration week to finish incomplete criteria before the internship demonstration.

## Phase 2 — October–December practical extension and security completion

**Outcome:** administrators maintain buildings/units/current assignments; residents submit private tickets and comment; administrators resolve them. Complete the semester's security requirements.

| Window | Deliverable | Evidence / exit |
| --- | --- | --- |
| October first half | Create, list and edit buildings and units; manage current resident assignments | UC-04 authorization tests, composite foreign-key checks and resident unit visibility |
| October second half | Ticket creation/list/detail/comments and two admin status transitions | UC-05/06 full private conversation and cross-user/cross-association tests |
| November first half | Mandatory local MFA enrollment/verification and supervised recovery | MFA bypass/replay/expiry/rate-limit tests; no pre-MFA sessions survive rollout |
| November second half | One OAuth 2.0/OIDC provider integration through .NET middleware | Authorization code flow with PKCE, state/nonce validation and rejection of tampered responses |
| December before final submission | Freeze business scope, fix security defects, encrypted backup restore, incident drill and presentation | Evidence matrix complete; known limitations written; reproducible final demo |

The university guidelines require MFA and OAuth 2.0 (pp. 1–2). The API handles the OAuth/OIDC sign-in flow on behalf of React and uses a session cookie for subsequent application requests. Confirm this integration meets the lecturer's assessment expectations before implementation. If bearer-token API access is required, update the authentication contract accordingly.

**Part-time planning assumption:** 4–6 focused hours per student per week, roughly 20–30 team hours/week, with 25% reserved for integration, tests and rework. Record real availability; if less, cut register polish and ticket UI extras first. Reserve time for security testing before adding interface improvements.

Detailed tasks, dependencies and completion criteria are in the [internship backlog](internship-backlog.md).

## Five-person ownership

| Student responsibility | Primary delivery | Peer reviewer |
| --- | --- | --- |
| A: backend authentication | Login, password hashing, sessions, later MFA | B |
| B: data and authorization | Migrations, memberships, register, tenant checks | A |
| C: frontend accounts | Forms, error/focus states, selector, profile | D |
| D: frontend/domain API | Users/register screens, tickets with B | C |
| E: test and delivery | API and browser security tests, CI, backup restoration and evidence | A/B for security |

Everyone writes tests and explains their own changes; E is not the only security owner. Pair on MFA/OIDC. Each week demonstrate one completed flow and one failed attack. Use one-week sprints in OpenProject. Assign phase 2 work-package IDs when creating those stories.

## Out of scope

Meters and utilities, financial journals and accounting, privacy-case workflows, assemblies and voting, document versioning, property-history engines, booking, work orders, notifications, file uploads and advanced reports are outside the September–December scope.
