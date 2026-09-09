# Locatarius

> Secure condominium governance and administration platform architecture and specifications.
>
> Aligned with Republic of Moldova Law 187/2022 (*Legea cu privire la condominiu*).

[![Security Focus](https://img.shields.io/badge/Security-DSA%20Standard-blue.svg)](docs/solution-design.md)

## Documentation Portal

The complete documentation is available in [`docs/`](docs/index.md) and is configured as a Material for MkDocs site through [`mkdocs.yml`](mkdocs.yml).

### Documentation Areas

- **Solution design:** Business context, scope classification, architecture, authorization matrix, UX, and non-functional requirements.
- **Shared patterns:** Authorization, transactions, errors, files, notifications, and delivery contracts.
- **Data model:** Entity ownership, constraints, relationships, and financial rules.
- **API catalogue:** Use-case-mapped operations, conventions, and representative contracts.
- **Use-case catalogue:** 55 stable use cases with dedicated specifications and Mermaid end-to-end sequences.
- **MVP roadmap:** Five production increments, dependencies, delivery gates, effort, and manual alternatives.
- **Operations and testing:** Test design, environments, cutover planning, ownership, and runbooks.
- **Decisions and risks:** ADRs, stakeholder questions, risks, and deferred decisions.
- **Traceability:** Requirement, use case, API, screen, entity, and acceptance-test coverage.
- **Validation report:** Documentation checks, diagram rendering results, consistency checks, and remaining assumptions.
- **Sources:** Verified primary references and external verification limits.

### Use-Case Coverage

The 55 use cases are grouped by domain:

| Domain | IDs | Count |
| :--- | :--- | ---: |
| Building management | `UC-BLD` | 3 |
| Communication | `UC-COM` | 5 |
| Finance | `UC-FIN` | 11 |
| Governance | `UC-GOV` | 3 |
| Identity and access | `UC-IAM` | 8 |
| Issues | `UC-ISS` | 6 |
| Maintenance | `UC-MNT` | 4 |
| Privacy | `UC-PRV` | 2 |
| Residents | `UC-RES` | 2 |
| Reporting | `UC-RPT` | 2 |
| Tenancy | `UC-TEN` | 4 |
| Utilities | `UC-UTL` | 5 |
| **Total** |  | **55** |

## Technology Direction

The specifications target the following implementation stack:

| Area | Planned technology or approach |
| :--- | :--- |
| Backend | ASP.NET Core .NET 8 Web API with Clean Architecture |
| Frontend | React or Next.js with Tailwind CSS and a mobile-first PWA approach |
| Database | PostgreSQL 16 with Entity Framework Core |
| Authentication | Managed OIDC or equivalent identity integration, MFA, sessions, and role/policy authorization |
| Security | Tenant isolation, least privilege, audit logging, rate limiting, CSRF protection, secure file handling, and restore-tested backups |
| Documentation | MkDocs Material, Markdown, Mermaid diagrams, and strict build validation |
| Delivery | Docker Compose for local services and GitHub Actions for documentation deployment |

These are implementation targets and architectural recommendations, not evidence that the application or infrastructure is already deployed.

## Local Documentation Setup

### Prerequisites

- Git.
- Python 3.x.
- `mkdocs-material`.
- Docker and Docker Compose only when working with the planned local database or application services.

### Serve the Documentation

```bash
git clone https://github.com/yoda-dinmd/Locatarius.git
cd Locatarius
git checkout feat/auth-user-mgmt
python3 -m venv .venv
.venv/bin/pip install mkdocs-material
.venv/bin/mkdocs serve
```

Open <http://127.0.0.1:8000/> after the server starts. The documentation workflow uses the same package and publishes with `mkdocs gh-deploy --force`.

### Database Service

The current Compose file defines PostgreSQL 16 and a future backend service. Until the .NET solution and `backend/Dockerfile` exist, start only PostgreSQL:

```bash
docker compose up -d postgres
```

Development database settings are defined in [`docker-compose.yml`](docker-compose.yml). They are local development credentials only and must not be reused in production.

## Repository Structure

```text
.
├── backend/                 # Reserved for the ASP.NET Core implementation
├── frontend/                # Reserved for the React/Next.js implementation
├── docs/                    # Architecture and product specifications
│   └── use-cases/           # 55 dedicated use-case specifications
├── .github/workflows/       # GitHub Actions documentation deployment
├── docker-compose.yml       # Local PostgreSQL and future backend services
├── mkdocs.yml               # Documentation site configuration
├── CONTRIBUTING.md          # Scrum, branching, commit, and PR rules
└── README.md                # Repository overview
```

## Contribution Workflow

Read [`CONTRIBUTING.md`](CONTRIBUTING.md) before making changes. Current rules include:

- One-week sprints beginning Mondays and daily OpenProject time logging.
- No direct pushes to `main` or `develop`.
- Sprint 1 task branches created from `feat/auth-user-mgmt`.
- Conventional Commits with an OpenProject work-package reference, for example `feat(auth): implement login endpoint and JWT issuance #62`.
- Pull requests with the `[#<WP-ID>] <type>(<scope>): <short description>` title pattern.
- At least one peer approval, clean CI, passing tests, and squash merging before integration.

## Branches

- `main`: Protected production and demonstration baseline.
- `develop`: Integration trunk.
- `feat/auth-user-mgmt`: Current Sprint 1 integration branch.

The current repository branch contains the documentation portal and project skeleton. Backend and frontend feature branches should be created from the Sprint 1 integration branch according to the contribution rules.

## Documentation Deployment

[`deploy-docs.yml`](.github/workflows/deploy-docs.yml) deploys the MkDocs site to GitHub Pages when documentation-related files change on `main` or `feat/auth-user-mgmt`. It can also be started manually with `workflow_dispatch`.

Configure GitHub Pages to use the `gh-pages` branch and the repository root after the workflow runs for the first time.

## Scope and Limitations

The documentation is a proposed implementation baseline, not application code, legal advice, a security certification, or proof of production readiness. Jurisdiction, privacy obligations, provider compatibility, capacity, budget, and financial or governance policies remain stakeholder decisions recorded in [`docs/decisions.md`](docs/decisions.md).
