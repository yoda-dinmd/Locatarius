# Sources and interpretation

## Project references

The university guidelines define assessment requirements. Domain research provides background and does not establish implementation requirements or legal compliance.

| Material | Used for | Limits |
| --- | --- | --- |
| **DAS_internship requirements.pdf**, 5 pages | pp. 1–2: encryption, MFA, OAuth 2.0, validation, XSS/CSP, CSRF/cookies, hashing, RBAC/backups. pp. 2–3: security documentation, incident response, presentation. pp. 3–5: September priorities, flexibility and five-point explanation for each security measure | No numeric password policy, exact HTTP status contract or three OpenProject story titles supplied; those are project design decisions |
| **app.pdf**, Project Foundation & Research Charter | Broad research framing, five-person team, competing product hypotheses, separation of assumptions from validated evidence | Exploratory research; product hypotheses require validation before they can become requirements |
| **Administratorul_de_bloc_Chisinau.pdf** | Association/resident domain vocabulary and examples of building problems | Legal/news/market claims in the research are not independently re-verified here; no legal rules or compliance claim are imported into the app |
| **Solutii Existente.pdf**, dated 4 Sep 2026 | Existing-solution comparison as background to the project's motivation | Competitor features are not university requirements and are not a parity backlog; claims are not re-verified |
| **Locatarius_Epic_66_Authentication_User_Management_2026-09-09_18-58.pdf** (epic export) | Confirms epic #66 name, Sprint 1, date range 7–11 Sep 2026 | Single page; does not include #62/#63/#64 descriptions or acceptance criteria |

The [latest OpenProject CSV snapshot](openproject/README.md) supplies the current recorded task descriptions and statuses. It includes 22 work packages, including #87–#90 and reopened #73 (In progress). It supersedes the September 9 export for task context. The updated export records the revised internship scope in #66 and deferral wording in #63/#64/#83, as summarized in the [internship backlog](internship-backlog.md). Status fields do not establish verified implementation.

## Technical references

- [ASP.NET Core 10 Identity configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0): configurable password hasher and authentication settings. This project explicitly configures its work factor instead of assuming defaults.
- [OWASP password storage](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html): PBKDF2 work-factor guidance. Our selected SHA512 work factor is 220,000 iterations; verify the actual library output.
- [ASP.NET Core antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0): framework CSRF token mechanisms.
- [ASP.NET Core OIDC web authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication?view=aspnetcore-10.0) and [OWASP OAuth guidance](https://cheatsheetseries.owasp.org/cheatsheets/OAuth2_Cheat_Sheet.html): code flow, PKCE and server-side web authentication integration.
- [OWASP MFA guidance](https://cheatsheetseries.owasp.org/cheatsheets/Multifactor_Authentication_Cheat_Sheet.html): MFA and recovery considerations; exact TOTP enrollment/recovery policy here is a project choice.
- [PostgreSQL 18 constraints](https://www.postgresql.org/docs/18/ddl-constraints.html): primary, unique, check and composite foreign keys.

Validation limits, screen behavior, pagination, schema and delivery estimates are project decisions. Implementation status and evidence are recorded in [traceability](traceability.md).
