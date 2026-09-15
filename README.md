# Locatarius

A security-focused building management application for a five-student, third-year TUM Development of Secure Applications PBL project.

**Target stack: .NET 10, PostgreSQL 18, React.** September delivers secure authentication and basic user management. October–December adds a small building/apartment register, private tickets/comments and the remaining university security controls.

Start with the [documentation guide](docs/index.md), [Sprint 1 acceptance criteria](docs/sprint-1.md), [exact database schema](docs/data-model.md) and [two-phase roadmap](docs/mvp-roadmap.md). The [seven use cases](docs/use-case-catalog.md) retain detailed stories and flows. [Security evidence and incident response](docs/operations-and-testing.md) explain how the work is assessed.

The application covers accounts, association access, a building/apartment register and private tickets. See the [scope boundary](docs/mvp-roadmap.md#out-of-scope) for excluded features.

## Repository status

The repository contains the application specification, a .NET backend scaffold and a reserved `frontend/` directory. Implementation progress and security evidence are tracked in [traceability](docs/traceability.md).

The backend currently exposes the template weather endpoint and `/health/live` (process liveness only). Database access and authentication are not wired up yet. No legal compliance or security certification is claimed.

## Run the local containers

Prerequisites: Docker Engine with Docker Compose v2 or newer, and `openssl`.

Create an ignored `.env` file once, without overwriting an existing one:

```bash
(test -e .env || (umask 077; printf 'POSTGRES_PASSWORD=%s\n' "$(openssl rand -hex 32)" > .env))
docker compose up --build --wait
curl --fail http://localhost:8080/health/live
docker compose down
```

Compose runs .NET 10 and PostgreSQL 18, with ports bound to localhost. Set `BACKEND_PORT` or `POSTGRES_PORT` in `.env` if the defaults (8080/5432) are occupied. Build the API alone with `docker build -t locatarius-api ./backend`.

The database uses the `pgdata18` volume mounted at `/var/lib/postgresql`, following the [PostgreSQL 18 image layout](https://hub.docker.com/_/postgres). `docker compose down` preserves it. An older `pgdata` volume is left untouched; migrate PostgreSQL 16 data using a tested dump/restore instead of attaching it to PostgreSQL 18. Changing `.env` does not change the password of an already initialized database.

This is a local HTTP scaffold. Before implementing browser authentication, configure trusted HTTPS and separate limited runtime/migration database identities as specified in [environment work package #78](docs/internship-backlog.md). The API receives no database owner credentials or sample JWT secrets.

Build all backend projects with `dotnet build backend/Locatarius.slnx --configuration Release`. The existing `backend/tests/PasswordHasherTests.cs` has no test project and is not included in the solution, so `dotnet test` does not currently validate those tests.

## Preview documentation

```bash
python3 -m venv .venv
.venv/bin/pip install mkdocs-material
.venv/bin/mkdocs serve
```

Build with `.venv/bin/mkdocs build --strict`. [mkdocs.yml](mkdocs.yml) groups scope, implementation and security evidence. The [documentation workflow](.github/workflows/deploy-docs.yml) publishes documentation from `main` to GitHub Pages.

Follow [CONTRIBUTING.md](CONTRIBUTING.md) for one-week sprints, OpenProject time logging, task branches, work-package references, peer review and squash merges. Create task branches from `main` and open reviewed PRs back to `main`; documentation checks do not replace application tests.
