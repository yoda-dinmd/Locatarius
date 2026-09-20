# Locatarius

A security-focused building management application for a five-student, third-year TUM Development of Secure Applications PBL project.

**Target stack: .NET 10, PostgreSQL 18, React.** The remaining September internship focuses on the original authentication and resident-creation tasks (#62–#64, #73–#90); further account-management features are deferred. October–December adds a small building/apartment register, private tickets/comments and the remaining university security controls.

Start with the [documentation guide](docs/index.md), [Internship acceptance criteria](docs/sprint-1.md), [exact database schema](docs/data-model.md) and [two-phase roadmap](docs/mvp-roadmap.md). The [seven use cases](docs/use-case-catalog.md) retain detailed stories and flows. [Security evidence and incident response](docs/operations-and-testing.md) explain how the work is assessed.

The application covers accounts, association access, a building/apartment register and private tickets. See the [scope boundary](docs/mvp-roadmap.md#out-of-scope) for excluded features.

## Repository status

The backend implements cookie login, CSRF and logout with PostgreSQL sessions.
The React login/password-change/dashboard flow remains a **localStorage mock**;
it is not connected to the real API. Use synthetic input only. Association policies,
server identity loading and real password change remain separate application work.

## Local containers and HTTPS

Prerequisites: Docker/Compose v2, Python 3, OpenSSL and mkcert. Follow the
[local HTTPS runbook](docs/local-https.md) for installation, browser trust,
network conflicts, existing data and troubleshooting.

```sh
python3 scripts/setup-local.py
CAROOT="$PWD/.local/ca" mkcert -install
docker compose config --quiet
docker compose up --build --wait --wait-timeout 180
python3 scripts/smoke-local.py --recreate
```

Open **https://localhost**. nginx is the only published application service;
backend/frontend run internally. PostgreSQL retains its loopback DB-tools port.
`HTTP_PORT`, `HTTPS_PORT`, `POSTGRES_PORT`, `DOCKER_SUBNET` and `NGINX_IP` in `.env`
control local conflicts. No Let's Encrypt or public domain is needed for this demo.

Setup creates ignored independent secrets and local certificates without replacing
existing configuration. Add missing keys from `.env.example` when upgrading an older
checkout. Install the local CA into your trust stores and restart the browser; the
smoke script's explicit CA verification does not install interactive browser trust.
Never commit `.env`, leaf keys or the CA key.

Startup runs `db-provision` → `db-migrator` → `db-grants` before the API. The migration
owner is not a superuser; the runtime role cannot modify EF migration history or
create tables. Existing PostgreSQL 18 data and seed passwords are preserved.
Optional demo seeding defaults off; enabling it requires `SeedDemoData__Password`.
All seed passwords must satisfy the application's creation-password rules.

```sh
docker compose ps -a
docker compose logs --tail 100 backend nginx db-migrator
docker compose down
```

`down` retains database and Data Protection volumes. Do not use `down --volumes`
on data you need to keep. Preserve older PostgreSQL 16 volumes and use an isolated,
tested dump/restore migration instead of an in-place image swap. See the runbook
for project names, protected secrets and existing-volume provisioning.

The smoke command verifies actual backend auth through nginx independently of the
mock UI, including cookie flags, CSRF, replay, rate limiting and service recreation.
It intentionally exhausts login throttling; wait a minute before another login test.
`/health/live` is process-only; internal `/health/ready` queries PostgreSQL.

Source verification (.NET 10, Node 22 and Docker):

```sh
dotnet build backend/Locatarius.slnx -c Release --warnaserror
dotnet test backend/Locatarius.slnx -c Release --no-build
npm ci --prefix frontend
npm run lint --prefix frontend
npm run build --prefix frontend
```

For direct migration-only execution, set `RUN_MIGRATIONS=true`,
`EXIT_AFTER_MIGRATIONS=true`, migration credentials and all required seed settings.
Normal API processes do not migrate. Table grants follow migrations as documented
in the runbook. Backend owners continue to supply migrations and seed behavior.

## Preview documentation

```bash
python3 -m venv .venv
.venv/bin/pip install mkdocs-material
.venv/bin/mkdocs serve
```

Build with `.venv/bin/mkdocs build --strict`. [mkdocs.yml](mkdocs.yml) groups scope, implementation and security evidence. The [documentation workflow](.github/workflows/deploy-docs.yml) publishes documentation from `main` to GitHub Pages.

Follow [CONTRIBUTING.md](CONTRIBUTING.md) for one-week sprints, OpenProject time logging, task branches, work-package references, peer review and squash merges. Create task branches from `main` and open reviewed PRs back to `main`; documentation checks do not replace application tests.
