# Decisions and open questions

## Accepted scope decisions

| Decision | Reason and consequence |
| --- | --- |
| Two business increments, seven use cases | Keeps delivery manageable for five students and leaves time for security testing |
| .NET 10 / PostgreSQL 18 / React | Shared implementation stack for the API, database and frontend |
| Association is isolation boundary | Membership role varies per association; ownership law is not modeled |
| Local framework password hashing and server sessions | Makes September password/security assessment concrete; no custom JWT issuer |
| Mandatory TOTP plus one OIDC provider in phase 2 | Preserves university MFA/OAuth expectations; tokens remain server-side; both sign-in methods share MFA, session and authorization checks |
| Admin can manage residents only | Avoids last-admin transfer and global credential privileges; trusted operator handles admin provisioning |
| No public registration or email delivery | Avoids mailbox verification, invitation and account-linking engines; temporary credentials require supervised handoff |
| Current unit assignments, independent of role | Both residents and admins can live in a unit and report tickets for it. Assignments do not change permissions or assert legal ownership |
| Three ticket states, plain text, no files | Keeps XSS/isolation demonstration while avoiding file scanning/storage lifecycle |
| App authorization + composite foreign keys | Simple, testable isolation; RLS not claimed as an implemented second layer |
| Logs in restricted rotating files | Records security events for investigation and assessment |

## Open decisions

1. Reconcile the exported implementation instructions before integration: JWT/bearer authentication (#75/#77/#85/#86), BCrypt/Argon2id selection (#74), Owner/Tenant/Admin roles (#73/#81/#83), name fields (#81), Problem Details (#80) and route paths. Review work already implemented with each owner. The documented cookie-session/PBKDF2/two-role contract is a proposed alignment target; a coherent alternative requires coordinated schema/API/UI/test updates. See [description review](internship-backlog.md#work-package-description-review).
2. Record the final submission date from the course schedule and confirm each student’s weekly availability.
3. Select the single OIDC provider with the mentor before November integration and confirm the BFF/session architecture meets the course's OAuth wording. Choose an accessible university/test provider supporting code+PKCE; do not build an authorization server. A provider enrollment dependency is a real delivery risk.
4. Record actual hosting encryption, key storage and student availability. Existing development Compose settings are not security evidence.

Known account-management limitation: the required global duplicate-email 409 lets an authorized admin test whether an email is already registered, although it discloses no association/profile. There is no anonymous registration route. Keep this tradeoff explicit in the security presentation.

Domain research provides context for the application. Legal compliance and production readiness require separate assessment. Features outside the semester scope are listed in the [roadmap](mvp-roadmap.md#out-of-scope).
