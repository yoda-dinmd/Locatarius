# Documentation validation

Documentation checks cover navigation, links, schema consistency and diagram syntax. Application security requires the separate tests listed in [operations and testing](operations-and-testing.md).

## Validation checklist

| Check | Scope |
| --- | --- |
| Site build | Build all 22 documentation pages with MkDocs strict mode |
| Navigation | Include every page and display UC-01 through UC-07 in the use-case sidebar labels |
| Diagrams | Parse all nine Mermaid diagrams: seven sequences, one architecture diagram and one entity-relationship diagram |
| Links | Check local page/file targets and section anchors in the generated site |
| Schema | Regenerate schema.sql from EF migrations and compare its PostgreSQL schema with a freshly migrated database |
| Acceptance coverage | Check 24 unique Sprint 1 case IDs, eight deferred account-management case IDs and 16 phase 2 case IDs |
| Formatting | Run `git diff --check` |

## Local setup

Run from the repository root:

```bash
python3 -m venv .venv
.venv/bin/pip install mkdocs-material
.venv/bin/mkdocs build --strict
.venv/bin/mkdocs serve
```

Use the virtual environment's executable so MkDocs loads Material and `pymdown-extensions` from the same installation. The latter provides `pymdownx.superfences`.

## Diagram checks

MkDocs preserves Mermaid source blocks for the browser to render. A successful site build therefore does not establish that diagram syntax is valid. Validate the source with Mermaid 11 and inspect diagrams in the served site.

Use commas or separate messages inside sequence labels. Mermaid treats a literal semicolon as a statement separator; a displayed semicolon must use `#59;`. See the [Mermaid sequence syntax reference](https://mermaid.js.org/syntax/sequenceDiagram.html#entity-codes-to-escape-characters).

## Application verification

On 18 September 2026, the SQL generated through
`20260917091807_AddSessionsAndLoginLockout` was applied to an empty PostgreSQL 18
database and compared with a second database initialized using `dotnet ef database
update`. Schema-only dumps (excluding PostgreSQL dump guard tokens) and both EF
migration-history rows matched. EF reported no pending model changes, and the
strict MkDocs build passed. The disposable databases were removed afterward.

This verifies the current SQL artifact, not association isolation or a complete
application acceptance run. Record broader results in [traceability](traceability.md),
including authorization, MFA/OIDC, encryption and backup restoration. Documentation
validation does not establish production readiness.
