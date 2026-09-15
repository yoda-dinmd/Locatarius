# Security, testing and university evidence

The university PDF expects a secure application (even with limited functionality), security documentation, a threat model/risk assessment, an incident response plan and a security-focused presentation. September selects core protections; the semester completes the remaining technical requirements. [Source details](sources.md) and [traceability](traceability.md) separate those requirements from product choices.

## Threat model and residual risks

Assets: passwords, session tokens, MFA seeds, resident names/emails, association register, private tickets, backup files and encryption keys. Attackers: anonymous internet client, malicious resident, compromised association admin and accidental developer/operator error. Trust boundaries: browser→API, API→database, API→identity provider, operator→deployment/backup. Admin compromise does not authorize another association.

| Risk | Impact / priority | Control | Required test / remaining limitation |
| --- | --- | --- | --- |
| Credential stuffing and password DB theft | Account compromise / high | Salted slow hashes, IP throttle, account lock, MFA | Wrong/unknown accounts, concurrent lockout, independent hash salts; lockout can cause denial of service |
| Forged roles / IDOR | Cross-association disclosure / high | Current membership and role lookup, scoped queries, composite FKs | Swap all route/body IDs, list counts and paging; application filtering remains essential |
| Stored XSS through names/comments | Session actions/data theft / high | React text rendering, CSP, no uploads/raw HTML | Script/HTML payload displayed as text; XSS would still be serious despite HttpOnly |
| CSRF/session theft | Unauthorized mutation / high | Framework tokens, Secure/HttpOnly cookies, TLS, idle/absolute expiry, revocation | Missing/mismatched token, cookie replay after logout/change, expired session |
| SQL injection/mass assignment | Data loss/escalation / high | Parameterized queries, explicit DTOs, schema constraints | Quote/SQL payloads stored harmlessly or rejected; `roleId`/scope payload rejected |
| MFA or OIDC bypass | Account compromise / high | Restricted sessions, replay protection, middleware verification | Password-only and OIDC-only sessions cannot access association routes; wrong issuer/state/nonce/PKCE rejected |
| Stolen disk/backup/key | Personal-data disclosure / high | Encrypted volume/backups, separate key storage, limited DB credentials | Restore requires authorized keys; encryption does not protect data from a compromised running API |
| Missing restore/incident practice | Prolonged loss/undetected compromise / medium | Tested backup, bounded logs, response drill | Timed isolated restore and cookie revocation; no formal production recovery SLA |

## Security implementation checklist

- **SEC-01 authentication:** implement shared password/session rules; persist only hash/digest. Two equal passwords must yield different stored hashes and both verify. No secrets in logs, source, responses or browser storage.
- **SEC-02 least privilege/isolation:** run all routes against admin A, resident A1, resident A2 in same unit, resident B and a dual-membership user. Test lists, totals, details and writes. Runtime DB role gets CONNECT, schema USAGE and necessary SELECT/INSERT/UPDATE/DELETE only; roles/association provisioning is operator-owned. Do not run the API using the database owner's credentials.
- **SEC-03 XSS/CSRF:** production CSP `default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'`. Use external CSS/scripts; no unsafe-inline/eval. Set `X-Content-Type-Options: nosniff`, `Referrer-Policy: no-referrer`. Use same-origin production assets; development hot reload may use a separate development-only policy. Confirm actual headers in a browser, not just configuration text.
- **SEC-04 transport/storage:** serve HTTPS (TLS 1.2 minimum, 1.3 preferred), redirect HTTP and enable HSTS on deployed HTTPS host. Use TLS with certificate verification for remote DB connections. Place PostgreSQL data on encrypted host/cloud storage and encrypt backups. Record actual encryption provider/algorithm/key custody. Protect MFA ciphertext with Data Protection; persist and encrypt its key ring outside DB and Git, accessible only to API identity. Password hashing is not “encryption at rest.” Whole-disk encryption does not make exports encrypted.
- **SEC-05 MFA:** enforce TOTP for all full sessions by final delivery, including provider-login users. Use maintained library and test vectors, QR/secret display only during enrollment, no secret logs/cache. Verify step replay and parallel verification atomically. Pending setup does not grant full access. For lost device, use supervised operator recovery below; no self-service bypass or home-grown recovery tokens.
- **SEC-06 OAuth:** implement [API phase 2 flow](api-catalog.md), explicitly set code/S256 PKCE, validate identity, discard tokens (`SaveTokens=false`), map only configured issuer+subject, then require app MFA. Test callback replay, changed state, wrong issuer/audience, expired signature, unknown subject and open redirects. Local logout ends only the Locatarius session; provider login can remain active, but a new Locatarius session still requires local MFA. Provider choice/configuration must be recorded before claiming this passed. [Microsoft OIDC guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication?view=aspnetcore-10.0) and [OWASP OAuth guidance](https://cheatsheetseries.owasp.org/cheatsheets/OAuth2_Cheat_Sheet.html).
- **SEC-07 logging/patching:** structured security logs contain UTC time, event code, correlation ID, actor UUID when known, association UUID where applicable and outcome. Include login failures, permission denials, access changes, recovery and successful login/logout; never request bodies, passwords, email as login identifier, cookies, tokens or MFA seeds. Store with restricted access and 14-day rotation for the demo; an attack burst is summarized/rate-limited to avoid log exhaustion. Operator reviews failed-auth/denial bursts before demos. Scan .NET/npm dependencies in CI, record findings and patch/retest relevant vulnerabilities.

## Tests and evidence

| Suite | What to run | Evidence |
| --- | --- | --- |
| Sprint 1 API | Every S1 ID, exact JSON/status assertions, failure rollback, duplicate/concurrent attempts | Test command/output, commit hash and date |
| PostgreSQL 18 | Apply migrations to empty test DB; positive inserts and intentional cross-association FKs, duplicate emails/units; transaction rollback | Engine version and integration test log; SQLite is not a substitute |
| Browser | Keyboard/focus/pending/error cases, XSS display, cookie flags, CSRF, association switch during requests | Short recording or checklist with actual observations |
| Phase 2 | UC-04–07 negatives and full reporter/admin conversation | API/browser tests plus failed attacker scenario |
| Delivery | Clean frontend/backend build, dependency findings, encrypted backup restore and incident rehearsal | Reproducible commands, outputs, observed duration, limitations |

For each control document five points required by the guidelines: **attack/problem; relevance here; implementation location; test and observed result; remaining limitations**. Use synthetic residents/tickets for assessment. This avoids needing real personal data for demonstrations; it is not a claim of legal compliance.

## Minimal deployment and backup runbook

1. Provision one API host and PostgreSQL 18 with separate runtime/migration credentials. Pin actual dependency versions in implementation lockfiles; set secrets in environment/secret storage. Persist protected Data Protection keys and enable encrypted disks. The existing Compose skeleton uses PostgreSQL 16, owner credentials and a nonexistent backend Dockerfile: it is not this deployment and must be corrected during implementation. Do not mount a PostgreSQL 16 data directory into PostgreSQL 18; use a tested dump/restore migration if data exists.
2. Apply migrations using migration role; seed two synthetic associations and distinct users with one-time passwords via an operator command reading secrets interactively. Start API with runtime role, then run smoke login/creation/isolation tests through HTTPS.
3. Nightly and before demo changes: create a consistent PostgreSQL logical backup using an operator backup role, encrypt it immediately with an established tool (for example age), copy to restricted storage outside the DB host, keep the latest seven daily encrypted copies. Back up the protected Data Protection key ring separately; store decryption keys outside backup storage. Never commit a dump or key.
4. Restore before each assessed milestone: choose a copy, decrypt only inside an isolated encrypted test volume, restore to an empty PostgreSQL 18 database, provide protected application keys, **delete all restored sessions**, then verify login, memberships, tickets and cross-association denial. Record counts, backup timestamp, duration and errors; remove temporary restored personal-data copies afterward. A backup existing is not proof restoration works.
5. Target for the classroom deployment: at most one day of data loss from nightly backups; measure actual recovery time without promising an untested RTO. On failed backup or restore, E investigates and records the gap before the next demonstration.

## Supervised recovery and provisioning

A restricted operator command (no web endpoint) creates associations/admins, links an existing user to another association, pre-links a verified OIDC issuer+subject, disables a compromised global user or resets credentials after identity verification. Use team-supervised identity verification for synthetic/demo accounts; no email-only recovery claim. Recovery requires one operator plus another student's recorded review. Verify intended association scope when adding membership; never change someone else's global password merely to add them to an association.

On lost password/MFA device, first verify the person out of band, then in one transaction invalidate all their sessions, set a newly hashed temporary password and `must_change_password=true`, clear MFA seed/enabled/last-step fields if MFA recovery is needed, and log a recovery event without secrets. Hand temporary credential directly to verified user; next login requires password change and fresh MFA enrollment. Operator access is a powerful residual risk; restrict host/DB access and never use shared demo credentials. An operator mistake can bypass normal application permissions and must be visible in the assessment discussion.

## Incident response drill

**Trigger:** unexpected cross-association access, repeated auth failures, disclosed secret or reported suspicious activity. E coordinates; A contains authentication issues; B checks data impact; C/D validate affected UI and document evidence. No automated messages to residents are part of this project.

1. Detect/triage: record UTC time, affected endpoint/account/association and symptom; reproduce using synthetic records without spreading leaked data. Preserve restricted logs and deployment/commit information. Severity is high for any confirmed data leak or authentication bypass.
2. Contain: disable affected global user or membership as appropriate, revoke sessions, or temporarily stop the affected endpoint/service if isolation is broken. Rotate exposed DB/provider/application secrets. Rotating encryption keys requires retaining authorized old keys for recovery; do not destroy evidence or backups.
3. Investigate: identify the missing check/configuration, query scoped logs for possible affected records, document uncertainty rather than invent an impact count. Escalate to mentor/course lead; real-data notification/legal decisions belong to the responsible organization, not an automatic student-app feature.
4. Eradicate/recover: patch, add a regression test reproducing the attack, peer review, deploy and retest with two associations. Restore only if data corruption requires it; revoke restored sessions.
5. Review: write one page covering timeline, root cause, containment, tests, impact/unknowns and prevention owner. Rehearse with a stolen synthetic session and show replay fails after revocation.

## Final demonstration (12–15 minutes)

Show the threat diagram; create resident and force password change; fail a wrong-password attempt; authenticate with MFA; demonstrate the actual OAuth/OIDC round trip; deny resident→admin and association A→B calls; report/comment/resolve one ticket; show hashes/ciphertext without displaying secrets, cookie/CSP headers, encrypted backup restore evidence and incident drill. Each student explains one control and its limitations. Record each control’s implementation status and include test results for completed controls.
