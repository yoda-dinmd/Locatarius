# Administrator issue triage demo (#95)

From the repository root, run `npm run dev --prefix frontend`, sign in with the
existing synthetic administrator `admin@locatarius.test` / `AdminPassword123!`,
and open the **Issues** tab at `/admin/issues`.

Drag a card between Open, In Progress and Closed, or open the card and use its
status selector. The selector works with keyboard and touch input. Closing an
issue sets `resolvedAt`; moving it out of Closed clears that value and updates
`updatedAt`. The detail panel also provides Start work, Close issue and Reopen
in progress buttons, plus priority editing for active issues. Search, priority
filter and date/priority sorting remain available. Click the backdrop, press
Escape or use Close details to leave the panel. Refresh resets every change.

All records are synthetic and held only in React memory. There is no issues API
in the backend yet, so mock changes are not server-authorized or persistent.
Gabriela’s resident reporting screen has its own scope and currently does not
share these fixtures. Victor can incorporate the `/admin/issues` link into the
team router; `/issues` remains available for the resident view.

## Alignment with the implemented database

EF Core entities, configurations and migrations are the schema authority. The
mock issue fields follow `Issue`: UUID `issueId`, `reportedBy` and `buildingId`,
title, description, integer status/priority, timestamps, reporter navigation
and attachments. Reporter name comes from `User.firstName` and `lastName`;
apartment number comes through `User.apartment`. Each attachment contains the
implemented ID, uploader, issue link, URL, file type and creation timestamp.
The water-meter attachment’s `fileUrl` is a local public asset so the demo works
offline. The small card label is derived from the UUID; it is not a ticket-number
column. There is no category, note, free-text location or issue comment field.

`IssueStatus` is Open=1, InProgress=2, Resolved=3; this UI calls value 3
**Closed**. `IssuePriority` is Low=1, Medium=2, High=3, Critical=4. The schema
has no status transition constraint, so the demo permits moves between all
three statuses, including Closed → In Progress. The older planned UC-06 document
says there is no reopening; Daniel’s later instruction calls for it in this
presentation demo. The eventual API contract needs a team decision before
backend integration. This branch does not claim the whole #95 story is complete.

The latest repository OpenProject CSV predates user story #95. Daniel supplied
an OpenProject screenshot naming it “Administrator Incident Triage & Resolution
Workflow,” without full description or acceptance criteria. The screenshot is
sufficient to reference #95 in the commit, but not to assert story completion.

## Verify

```sh
npm run lint --prefix frontend
npm run build --prefix frontend
node --experimental-strip-types --test frontend/src/features/issue-triage/issueModel.test.mjs
```

The model tests check UUID/relationship consistency, status movement, resolution
and reopening timestamps, unchanged-state drops, priority edits and fixture
immutability. Browser verification covered desktop drag-and-drop, Closed → In
Progress, the status selector, the photo, and 360px layout.

## Photo credit

`frontend/public/issue-demo/broken-water-meter.jpg` is a 960px derivative of
[Broken water meter shows measurement in residential area](https://commons.wikimedia.org/wiki/File:Broken_water_meter_shows_measurement_in_residential_area.jpg)
by Shixart1985, licensed [CC BY 2.0](https://creativecommons.org/licenses/by/2.0/).
It is an illustrative sample, not an actual resident upload. The detail panel
shows its attribution and license.
