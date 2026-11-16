# OpenProject work package snapshots

Latest snapshot: [2026-11-16 work packages](2026-11-16/work-packages.csv).

This CSV contains 15 open work packages (`In progress` or `New`) for `Sprint Final: Security & E2E Integration`: epic #66, stories #62–#64, and the 11 active sprint tasks (#76, #77, #81, #82, #86, #89, #116, #117, #118, #119, #120). Closed work packages from earlier sprints (#73–#75, #78–#80, #83–#85, #87, #88, #90, #92–#97) were filtered out during export. It records IDs, subjects, types, statuses, assignees, priorities and full descriptions. It does **not** include parent/child relations or sprint/version fields. OpenProject remains the live planning source; this export records its state at export time.

## Snapshot history and current scope

- [Latest export, 16 November 2026](2026-11-16/work-packages.csv): 15 open work packages (#62–#64, #66, #76, #77, #81, #82, #86, #89, #116–#120), adding #120 (`[BE/CI] Resolve NuGet NU1902/NU1903 vulnerabilities in backend projects`).
- [15 November 2026 export](2026-11-15/work-packages.csv): 14 open work packages (#62–#64, #66, #76, #77, #81, #82, #86, #89, #116–#119) with updated anti-hallucination specifications aligned with the actual codebase (`Building.AdminId` tenant isolation, `/api/residents`, `/api/apartments`, `/api/issues`).
- [16 September 16:13 export](2026-09-16-1613/work-packages.csv), source `OpenProject_Work_packages_2026-09-1620260916-53139-gujmkm.csv`: 22 work packages including #73, #74, #75, #78, #90.
- [Earlier 16 September export](2026-09-16/work-packages.csv), source `OpenProject_Work_packages_2026-09-1620260916-53139-25n336.csv`: retained unchanged for history.

The directory suffix distinguishes same-day snapshots using the supplied file's
local modification time; it is not a server export timestamp. Sprint dates,
activation and board configuration cannot be verified from this export because
it has no Sprint or Parent fields.

## Find your task

Read the assigned ID's full description and its referenced dependencies before
working. Use the latest snapshot (`docs/openproject/2026-11-16/work-packages.csv`) for active tasks, or `docs/openproject/2026-09-16-1613/work-packages.csv` for historical closed tasks (#73, #74, #75, #78, #90). A Closed status alone does not establish that every requirement is implemented.

Use a CSV parser rather than splitting lines: descriptions span multiple lines
and may contain commas and HTML entities. From the repository root:

```python
import csv
import html
from pathlib import Path

snapshot = Path("docs/openproject/2026-11-16/work-packages.csv")
task_id = "77"
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
