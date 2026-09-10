# Locatarius

A security-focused building management application for a five-student, third-year TUM Development of Secure Applications PBL project.

**Target stack: .NET 10, PostgreSQL 18, React.** September delivers secure authentication and basic user management. October–December adds a small building/apartment register, private tickets/comments and the remaining university security controls.

Start with the [documentation guide](docs/index.md), [Sprint 1 acceptance criteria](docs/sprint-1.md), [exact database schema](docs/data-model.md) and [two-phase roadmap](docs/mvp-roadmap.md). The [seven use cases](docs/use-case-catalog.md) retain detailed stories and flows. [Security evidence and incident response](docs/operations-and-testing.md) explain how the work is assessed.

The application covers accounts, association access, a building/apartment register and private tickets. See the [scope boundary](docs/mvp-roadmap.md#out-of-scope) for excluded features.

## Repository status

The repository contains the application specification and reserved `backend/` and `frontend/` directories. Implementation progress and security evidence are tracked in [traceability](docs/traceability.md).

The existing [Compose skeleton](docker-compose.yml) still uses PostgreSQL 16 and references a missing backend Dockerfile. Align it with PostgreSQL 18 and separate runtime credentials when implementing; do not reuse its sample credentials or JWT secret. No legal compliance or security certification is claimed.

## Preview documentation

```bash
python3 -m venv .venv
.venv/bin/pip install mkdocs-material
.venv/bin/mkdocs serve
```

Build with `.venv/bin/mkdocs build --strict`. [mkdocs.yml](mkdocs.yml) groups scope, implementation and security evidence. The [documentation workflow](.github/workflows/deploy-docs.yml) publishes documentation from the configured branches.

Follow [CONTRIBUTING.md](CONTRIBUTING.md) for one-week sprints, OpenProject time logging, task branches, work-package references, peer review and squash merges. Implementation work belongs in reviewed feature branches; documentation checks do not replace application tests.
