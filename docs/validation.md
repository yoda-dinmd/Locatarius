# Documentation validation

Documentation checks cover navigation, links, schema consistency and diagram syntax. Application security requires the separate tests listed in [operations and testing](operations-and-testing.md).

## Validation checklist

| Check | Scope |
| --- | --- |
| Site build | Build all 22 documentation pages with MkDocs strict mode |
| Navigation | Include every page and display UC-01 through UC-07 in the use-case sidebar labels |
| Diagrams | Parse all nine Mermaid diagrams: seven sequences, one architecture diagram and one entity-relationship diagram |
| Links | Check local page/file targets and section anchors in the generated site |
| Schema | Confirm that the ten-table SQL file matches the DDL displayed on the database page |
| Acceptance coverage | Check 24 unique Sprint 1 case IDs, eight Sprint 2 account-management case IDs and 16 phase 2 case IDs |
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

PostgreSQL 18 migration execution and application acceptance tests remain unverified. Record implementation results in [traceability](traceability.md), including authentication, authorization, MFA/OIDC, encryption and backup restoration. Documentation validation does not establish production readiness.
