# Locatarius

A security-focused building management application for a five-student, third-year TUM Development of Secure Applications PBL project.

**Target stack: .NET 10, PostgreSQL 18, React.** The remaining September internship focuses on the original authentication and resident-creation tasks (#62–#64, #73–#90); further account-management features are deferred. October–December adds a small building/apartment register, private tickets/comments and the remaining university security controls.

Start with the [documentation guide](docs/index.md), [Internship acceptance criteria](docs/sprint-1.md), [exact database schema](docs/data-model.md) and [two-phase roadmap](docs/mvp-roadmap.md). The [seven use cases](docs/use-case-catalog.md) retain detailed stories and flows. [Security evidence and incident response](docs/operations-and-testing.md) explain how the work is assessed.

The application covers accounts, association access, a building/apartment register and private tickets. See the [scope boundary](docs/mvp-roadmap.md#out-of-scope) for excluded features.

## Repository status

The backend implements cookie login, CSRF and logout with PostgreSQL sessions.
The React login/password-change/dashboard flow is currently a **localStorage mock**;
it does not authenticate against that backend. Do not enter real passwords into it.
Association memberships, server-side identity loading and password-change completion
remain outstanding. See the [integration review](docs/reviews/2026-09-20-consistency.md).

## Run the local containers

Prerequisites: Docker Engine with Compose v2 and `openssl`.
Copy `.env.example` to an ignored `.env` without overwriting existing configuration.
Generate separate values using `openssl rand -hex 32` for `POSTGRES_PASSWORD`,
`RUNTIME_DB_PASSWORD` and `SeedAdmin__Password`. Fill the administrator's email and
names. Demo data is disabled by default; if enabled, supply a separately generated
`SeedDemoData__Password`. Seed passwords must be 15–128 characters without controls.

```sh
docker compose --env-file .env config --quiet
docker compose --env-file .env up --build --wait --wait-timeout 120
docker compose ps -a
curl --fail http://localhost:8080/health/live
curl --fail --head http://localhost/
docker compose logs backend db-migrator nginx
docker compose down
```

The stack contains four long-running services (`postgres`, `backend`, `frontend`,
`nginx`) and a one-shot `db-migrator`. On an empty database, PostgreSQL's init script
creates `locatarius_app`; the migrator applies EF migrations and admin/optional demo
seeding as `locatarius_admin`, then exits successfully before the backend starts.
The backend uses runtime credentials and does not apply migrations. Admin and demo
passwords are not reset by repeat seeding. The runtime role has broad DML grants on
public tables; narrowing these privileges remains outstanding.

Current URLs: nginx HTTP at `http://localhost`, backend liveness at
`http://localhost:8080/health/live`, and the static frontend at `http://localhost:3000`.
Set `BACKEND_PORT`, `FRONTEND_PORT` and `POSTGRES_PORT` in `.env` for local conflicts.
nginx currently publishes all-interface ports 80/443, but **443 has no TLS listener**.
Secure-cookie authentication requires the pending HTTPS/proxy configuration. This
is a local integration scaffold, not a deployment-ready HTTPS stack. Frontend deep
links also need SPA fallback in its serving nginx configuration.

`/health/live` checks the API process only. It does not prove database access,
HTTPS, browser authentication or authorization. See the review for verified checks.

`docker compose down` retains the `pgdata18` volume mounted at `/var/lib/postgresql`.
Never use `down --volumes` to repair an existing database you need to keep. PostgreSQL
init scripts run only on first initialization: an older existing volume needs the
runtime role/grants provisioned separately before this new backend can connect.
After adding `RUNTIME_DB_PASSWORD` to its protected configuration, the operator can
apply the init SQL **once, only if the role is absent**:

```sh
docker compose exec -T postgres psql -U locatarius_admin -d locatarius_db \
  -v ON_ERROR_STOP=1 -f /docker-entrypoint-initdb.d/init-runtime-user.sql
```

If the role already exists, review and alter its password/grants explicitly instead.
Changing `.env` does not change database passwords. Preserve older PostgreSQL 16
volumes and use tested dump/restore migration rather than reusing their data directory.

Backend verification requires .NET 10 and Docker for disposable PostgreSQL tests:

```sh
dotnet build backend/Locatarius.slnx -c Release --warnaserror
dotnet test backend/Locatarius.slnx -c Release --no-build
npm ci --prefix frontend
npm run lint --prefix frontend
npm run build --prefix frontend
```

For direct API execution, explicitly set `RUN_MIGRATIONS=true` with migration
credentials and seed settings when initialization is needed. Use
`EXIT_AFTER_MIGRATIONS=true` for a migration-only invocation. Normal API processes
leave both flags false. Existing application tests run through the solution.

## Preview documentation

```bash
python3 -m venv .venv
.venv/bin/pip install mkdocs-material
.venv/bin/mkdocs serve
```

Build with `.venv/bin/mkdocs build --strict`. [mkdocs.yml](mkdocs.yml) groups scope, implementation and security evidence. The [documentation workflow](.github/workflows/deploy-docs.yml) publishes documentation from `main` to GitHub Pages.

Follow [CONTRIBUTING.md](CONTRIBUTING.md) for one-week sprints, OpenProject time logging, task branches, work-package references, peer review and squash merges. Create task branches from `main` and open reviewed PRs back to `main`; documentation checks do not replace application tests.
