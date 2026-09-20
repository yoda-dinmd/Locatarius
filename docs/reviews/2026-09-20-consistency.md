# Container and CI consistency review — 20 September 2026

Reviewed `origin/main` at `28ba1b6` and the Antigravity DevOps branch
`feat/devops-task-78-new` at `dfa31e3`. Changes were checked out in a separate
worktree to preserve the original checkout's uncommitted `.gitignore` edit.
This review concerns infrastructure and API integration, not a visual UX audit.

## CI failures corrected

1. `Program.cs` imported both SameSiteMode enums. The ASP.NET cookie assignment
   could not compile (CS0104). Importing only a `HeaderNames` alias preserves
   Retry-After handling and removes the competing enum from scope.
2. `.env` was generated after compilation, so a build failure left Compose log
   collection without required interpolation values. Environment generation now
   precedes the build; log collection is guarded and cleanup explicitly uses the
   generated file. Cleanup errors are not hidden with `|| true`.
3. The new opt-in `RUN_MIGRATIONS` flag skipped database initialization in API HTTP
   tests. Their factory now explicitly enables migration/seeding and disables
   migration-only exit, preserving the runtime API's default of not migrating.
4. The `.gitignore` exception for `.env.example` contained embedded NUL bytes.
   It is now ordinary UTF-8 text. Committed example/default seed passwords were
   replaced with required protected inputs; demo seeding defaults to disabled.
5. On this SELinux host, PostgreSQL could not read its new SQL bind mount.
   Read-only bind mounts now request container labeling (`ro,Z`), including nginx's
   configuration. No database volume was renamed or deleted to resolve this.

## Remaining integration findings

### P1 — HTTPS authentication is not connected

`nginx/nginx.conf` has only `listen 80`; Compose publishes 443 but mounts no
certificate. There is no HTTPS redirect. Backend `Proxy__TrustedProxy` is unset,
so forwarded headers are ignored. HTTP `/api/auth/csrf` cannot issue the required
Secure cookie. This remains inconsistent with the backend integration contract
and #78's HTTPS requirement, even when the CI HTTP smoke test passes.

Complete TLS with SAN-bearing development certificates and deployment certificates,
configure one stable nginx address after checking VM networks, and set the exact
trusted proxy IP. Overwrite forwarded client headers at the single public edge.
Remove backend/frontend host publications and verify redirects, cookie flags,
spoofed headers, and real login/logout through trusted HTTPS. Port publication alone
and a successful static-page response do not establish this behavior.

### P1 — The frontend login flow is a mock

`frontend/src/auth/auth.ts` uses hardcoded demo users, plaintext password comparison
and localStorage for users, passwords and simulated sessions. It sends no backend
CSRF/login/logout/identity requests. A frontend dashboard therefore proves no
server authentication or authorization. #77 requires server-owned cookie sessions
and identity restoration, not browser-owned role/session state. Keep real credentials
out of this prototype and implement the shared API client with #83/#87 before
claiming an integrated login/password-change journey.

### P2 — Static frontend deep links lack SPA fallback

The frontend Dockerfile serves Vite output using nginx's default configuration.
React navigates with full-page `/dashboard` and `/change-password` requests, but
those paths are not physical files and currently return nginx 404. Add a frontend
server configuration with `try_files $uri $uri/ /index.html`, leaving API routing
at the edge, and verify direct navigation and refresh on both paths.

### P2 — Existing volumes do not receive the new runtime role

`docker/postgres/init-runtime-user.sql` runs only during first PostgreSQL
initialization. Existing `pgdata18` volumes need explicit role/grant provisioning;
simply changing `.env` and restarting cannot create the role or rotate its password.
README now describes the boundary and the one-time existing-volume procedure.
Do not discard existing data to make startup pass.

### P2 — Runtime permissions and migration credentials need further narrowing

The API now uses `locatarius_app`, but its default privileges grant DML on **all**
public tables, including `__EFMigrationsHistory`. It should not modify migration
bookkeeping. The migration identity remains `POSTGRES_USER`, which the official
image creates as a bootstrap superuser, rather than a dedicated limited migration
owner. Follow up with explicit business-table grants and privilege tests, keeping
provisioning separate from ordinary migration execution. Current role separation
is useful but does not complete least-privilege verification.

### Documentation and requirement boundaries

README incorrectly claimed an empty frontend, a weather-only API, no database/auth
integration, and no runnable tests. Its environment setup omitted the runtime DB
password. Those statements and the startup/migration instructions are corrected.
The operations runbook claimed association seeding, although EF still has global
roles and no association/membership tables; that claim is corrected. Session-backed
backend auth, frontend mocks and planned deployment behavior are now distinguished.
#73, #77, #78, #83 and #87 still have integration requirements; this patch does not
mark them complete. Persistent protected Data Protection keys and VM certificate/
network decisions also remain pending.

## Verification of the corrected working tree

- .NET Release build with warnings as errors: zero errors/warnings; 59 tests passed,
  zero skipped. Testcontainers used disposable PostgreSQL 18 databases.
- Frontend `npm ci`, lint and production build passed. Its Docker build succeeded
  on retry after a transient npm network error.
- Strict MkDocs build and `git diff --check` passed.
- Disposable Compose stack started successfully: PostgreSQL/backend healthy,
  frontend/nginx running, migrator exited 0. Local test overrides used loopback
  ports 28081/28443 (nginx), 28082 (backend), 23000 (frontend), 25433 (database).
- Runtime-role TCP authentication succeeded, read eight seeded users, and could
  not CREATE in public. It **could UPDATE migration history**, confirming the
  remaining grant issue above.
- Actual route probes: backend `/health/live` 200; nginx `/` 200;
  `/dashboard` and `/change-password` 404; `/api/auth/csrf` 500 over HTTP;
  HTTPS connection on published 443 failed the TLS handshake (curl exit 35).
- Disposable stack/volume removed after inspection. Existing development volumes
  were not used. No hosted GitHub rerun is claimed until these commits are pushed.

The first two route checks satisfy the current workflow's container smoke checks.
Their success must not be presented as completion of the HTTPS/auth acceptance
criteria. Complete the TLS/network and frontend API integration work next.
