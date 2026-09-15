# Requirement-to-evidence traceability

Implementation and evidence items below are **planned / not verified**. For each completed item, record the commit, test command, execution date, observed result and evidence link. Documentation validation and application security testing are separate checks.

## Product contract

| Requirement | Story/use case | UI / API | Tables | Acceptance |
| --- | --- | --- | --- | --- |
| Secure login/session/logout | Sprint 1 #62 and #64 / [UC-01](use-cases/UC-01.md) | Sign in, selector / auth routes | users, sessions, memberships, associations | S1-62-01–11 |
| Admin creates residents | Sprint 1 #63 / [UC-02](use-cases/UC-02.md) | Users / scoped users routes | users, memberships, roles | S1-63-01–05 |
| Registered user authenticates and accesses permitted pages | Sprint 1 #64 / [UC-01](use-cases/UC-01.md) | Login, protected routes / auth me/logout | users, sessions, memberships | S1-64-01–08 |
| Admin lists and controls resident access | Sprint 2 S2-US1 / [UC-02](use-cases/UC-02.md) | Users / scoped users routes | users, memberships | S2-ADM-01–04 |
| Own profile/password management | Sprint 2 S2-US2 / [UC-03](use-cases/UC-03.md) | Profile / auth me/password | users, sessions | S2-PRO-01–04 |
| Building/unit register and current assignments | [UC-04](use-cases/UC-04.md) | Register, My units / buildings, units, residents | buildings, units, unit_memberships | P2-04-01–04 |
| Private ticket reporting | [UC-05](use-cases/UC-05.md) | Tickets / tickets list/create/detail | tickets, unit_memberships, memberships | P2-05-01–04 |
| Private discussion and resolution | [UC-06](use-cases/UC-06.md) | Ticket detail / comments, status | tickets, comments, memberships | P2-06-01–04 |
| MFA/provider login | [UC-07](use-cases/UC-07.md) | Auth steps / mfa and oidc routes | users, sessions | P2-07-01–04 |

## University guidelines coverage

Source: **DAS_internship requirements.pdf**. The technical requirements apply across the semester; pp. 3–5 explicitly permit selecting a subset for September. Numeric/API/product details are project decisions.

| University requirement / page | Planned phase / implementation | Proof required |
| --- | --- | --- |
| Encryption at rest/in transit, pp. 1–2 | Phase 1 HTTPS; phase 2 encrypted DB volume, backups and protected MFA keys, SEC-04 | Actual TLS headers/certificates/config, encryption/key-custody evidence, authorized decryption/restore |
| Secure hashing, p. 1 | Phase 1 framework salted PBKDF2, SEC-01 | Correct/wrong password verification; two equal passwords yield distinct hashes |
| MFA, p. 1 | Phase 2 local TOTP on both login paths, UC-07 / SEC-05 | Enrollment, invalid/replayed/rate-limited codes, no first-factor-only business access |
| OAuth 2.0, p. 1 | Phase 2 one provider code+PKCE/OIDC flow, SEC-06 | Actual flow and tamper rejection; record lecturer's acceptance of BFF/session interpretation |
| Input validation, secure errors, p. 1 | Phase 1 DTO rules/envelope; extend for phase 2 | S1 exact JSON/length/format tests; no secret/stack leakage |
| XSS/CSP, pp. 1–2 | Phase 1 React escaping and CSP, SEC-03 | Browser script payload inert; delivered CSP and headers |
| CSRF/secure cookies, p. 2 | Phase 1 antiforgery/session rules | Missing/mismatched-token rejection, cookie flags and replay tests |
| SQL injection/session timeout, p. 2 | Phase 1 parameterized queries and server sessions | Injection inputs and expiry/logout/password-change tests |
| Patching, p. 2 | Phase 1 CI scans then maintain, SEC-07 | Dependency scan, disposition and patch/retest record |
| RBAC, p. 2 | Phase 1 role/membership checks and limited DB runtime role | Resident→admin and A→B failures; DB grants inspection |
| Backup/recovery, p. 2 | Initial September restore; encrypted final restore, operations runbook | Timed fresh-DB restore, expected records, invalidated restored sessions |
| Threat model/risk assessment, p. 2 | Phase 1 threat table; extend with MFA/OIDC/tickets | Reviewed threats, control→test links, residual risks |
| Incident response, p. 2 | Initial plan then phase 2 drill | Timeline, containment/revocation, root cause, regression test |
| Presentation and control explanation, pp. 2–5 | September and final demo | Each control: problem, relevance, implementation, test, limitations; all five students explain contributions |

The guidelines' illustrative CAPTCHA/uploads are not selected features. Their presence in a list of possible controls does not require building file handling or bot challenges. Required semester MFA/OAuth entries are retained separately above.
