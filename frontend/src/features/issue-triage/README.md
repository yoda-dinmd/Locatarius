# Administrator issue triage board (#95, live API since #118)

Start the stack (see the repository README), sign in as an administrator and
open the **Issues** tab at `/admin/issues`.

The board loads `GET /api/issues`, which returns only issues of buildings the
signed-in administrator owns. Drag a card between Open, In Progress and Closed,
or open the card and use its status or priority selector; the selectors work
with keyboard and touch input. Each change is shown immediately and sent as
`PATCH /api/issues/{id}`. The server's copy then replaces the card; if the
server refuses (for example `409 ISSUE_RESOLVED` or `404 ISSUE_NOT_FOUND`), the
card returns to its previous state and an alert explains why. While a change is
saving, that card cannot be dragged or edited again. Changes persist in
PostgreSQL and survive a page refresh.

Closing an issue sets `resolvedAt`; moving it out of Closed clears that value.
Priority is locked while an issue is Closed. Search, priority filter and
date/priority sorting work on the loaded issues. Click the backdrop, press
Escape or use Close details to leave the panel.

Residents use `/issues`, `/issues/new` and `/issues/{id}`, which go through the
same API facade in `frontend/src/auth/issues.ts`.

## Code structure

- `frontend/src/auth/issues.ts` is the only module that knows the `/api/issues`
  URLs. It validates each response against `IssueDto` and turns API errors into
  user-facing messages (`describeIssueError`).
- `frontend/src/hooks/useAsyncResource.ts` gives every page the same
  loading / error / ready states and cancels requests on unmount.
- `IssueTriage.tsx` describes each board change as an `IssueCommand`
  (optimistic `apply`, the PATCH `changes`, a success message) and runs it with
  a snapshot of the issue for rollback.
- `issueModel.ts` holds the shared `Issue` type and the pure transition helpers
  used for the optimistic update.

## Alignment with the implemented database

EF Core entities, configurations and migrations are the schema authority. The
`Issue` type mirrors `IssueDto`: UUID `issueId`, `reportedBy` and `buildingId`,
`buildingNumber`, title, description, integer status/priority, timestamps,
reporter (first/last name and apartment number) and attachments. The small
card label is derived from the UUID; it is not a ticket-number column. There is
no category, note, free-text location or issue comment field.

`IssueStatus` is Open=1, InProgress=2, Resolved=3; this UI calls value 3
**Closed**. `IssuePriority` is Low=1, Medium=2, High=3, Critical=4. The API
allows moves between all three statuses, including reopening.

The API has no file upload and no fields for fix notes. Residents therefore
report issues with a title and description only, and the admin close page is a
confirmation that sets status 3. Existing attachments (such as seeded demo
data) are still displayed.

## Verify

```sh
npm run lint --prefix frontend
npm run build --prefix frontend
npm test --prefix frontend
node --experimental-strip-types --test frontend/src/features/issue-triage/issueModel.test.mjs
npm run test:navigation --prefix frontend
```

`issuesMock.json` is now only a fixture for `issueModel.test.mjs`, which checks
UUID/relationship consistency, status movement, resolution and reopening
timestamps, unchanged-state drops and priority edits. `IssueTriage.test.tsx`
covers loading, the optimistic PATCH, rollback on rejection and load-error
retry.

## Photo credit

`frontend/public/issue-demo/broken-water-meter.jpg` is a 960px derivative of
[Broken water meter shows measurement in residential area](https://commons.wikimedia.org/wiki/File:Broken_water_meter_shows_measurement_in_residential_area.jpg)
by Shixart1985, licensed [CC BY 2.0](https://creativecommons.org/licenses/by/2.0/).
It is an illustrative sample used by the test fixture, not an actual resident
upload.
