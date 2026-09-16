# Repository instructions for agents

## Read the assigned work package first

- Read `CONTRIBUTING.md` before creating branches, commits or pull requests.
- Read `docs/openproject/README.md` and use its explicitly designated latest CSV
  snapshot to find the assigned OpenProject work package by ID.
- Read the complete description, acceptance criteria and dependencies, plus any
  referenced work packages and parent story available in the export. Do not infer
  scope from the title, assignee or status alone.
- Parse CSV with a CSV-aware reader (for example Python's `csv.DictReader`):
  descriptions contain quoted commas and embedded newlines. Decode HTML entities
  for reading when needed, but preserve the original export.
- Some exports omit parent/child relations. Do not invent them; consult explicit
  task references and repository context, and ask for missing information only
  when it is necessary to proceed.
- Treat the export as a dated requirements snapshot, not proof of implemented or
  tested behavior. Follow explicit user updates and report discrepancies between
  task requirements, documentation and code. Do not silently remove requirements
  to match the implementation.
- Follow `CONTRIBUTING.md` branch naming and include the assigned work package ID
  in commits and PR titles. Do not mark a whole work package complete when only a
  supporting change has been delivered.

## Updating work package context

Store only CSV exports under `docs/openproject/`, following its README. Update the
latest-snapshot link whenever a new export is added. Keep older snapshots for
history; do not choose an export based on filesystem modification time.
