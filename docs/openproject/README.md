# OpenProject work package snapshots

Latest snapshot: [2026-09-16 work packages](2026-09-16/work-packages.csv).

This CSV contains 22 work packages: epic #66, stories #62–#64 and tasks #73–#90,
including closed task #73. It records IDs, subjects, types, statuses, assignees,
priorities and full descriptions. It does **not** include parent/child relations
or sprint/version fields. OpenProject remains the live planning source; this
export records its state at export time.

Source filename: `OpenProject_Work_packages_2026-09-1620260916-53139-25n336.csv`.
The stored file is an unchanged copy of the supplied export.

## Find your task

Read the assigned ID's full description and its referenced dependencies before
working. For example, #78 is Victor Gafenco's DevOps task, #89 is the database
documentation task and #90 is the database initialization/seeding task. A Closed
status alone does not establish that every requirement is implemented.

Use a CSV parser rather than splitting lines: descriptions span multiple lines
and may contain commas and HTML entities. From the repository root:

```python
import csv
import html
from pathlib import Path

snapshot = Path("docs/openproject/2026-09-16/work-packages.csv")
task_id = "78"
with snapshot.open(encoding="utf-8-sig", newline="") as source:
    tasks = {row["id"]: row for row in csv.DictReader(source)}
task = tasks[task_id]
print(task["Subject"])
print(html.unescape(task["Description"]))
```

## Add a new export

1. Export CSV with IDs, full descriptions and all relevant work packages,
   including closed tasks and dependencies. Include parent IDs/relations when
   available; describe any omissions here.
2. Save the original CSV as `docs/openproject/YYYY-MM-DD/work-packages.csv`.
   For another export on the same date, use a new directory such as
   `YYYY-MM-DD-HHMM` instead of overwriting the earlier snapshot.
3. Update the latest-snapshot link and coverage summary above. Preserve older
   snapshots and record the original source filename for the new export.
4. Verify the CSV parses, IDs are unique and descriptions are present. Preserve
   source text; decode entities only when displaying it.

Use CSV only; no XLS copy is needed. These files are included in the published
documentation, so exports must contain only information intended to be shared.
