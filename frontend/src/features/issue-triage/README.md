# Daniel’s administrator issue triage demo

## Review and run

From the repository root, run `npm run dev --prefix frontend` and open the URL
printed by Vite. Sign in with the existing synthetic administrator:
`admin@locatarius.test` / `AdminPassword123!`. Select the **Issues** tab (or open
`/admin/issues`) for `IssueTriage`; **Dashboard** stays a separate welcome page.
No backend or database is required.

Select **Broken water meter**, inspect the bundled photo, click **Start work**,
then **Close issue**. The report moves through Open → In Progress → Closed and
updates the stage counts. Change an active report’s priority and filter by
priority. Search matches reference, title, description, resident and apartment.
Sort cards by priority or creation date. Closed reports stay readable with controls
disabled. The detail
dialog closes with its button, Escape or a click on the backdrop. Refresh resets
all five fixtures and edits.

State is held in React memory only. This is a presentation demo, not API-backed
issue management or server authorization. Existing mock login controls the admin
view. All people, reports and building data here are synthetic.

## Scope and integration

New files are isolated in `frontend/src/features/issue-triage/`. The shared-file
changes are the administrator-only `/admin/issues` route in `App.tsx`
and the Issues navigation/mount in `pages/Dashboard.tsx`. The `/issues` path is
left available for Gabriela’s resident view. Victor can incorporate the link into
his eventual shared router. Real identity and server authorization still need
integration when the backend supports issues.

No dependency, auth, backend, resident reporting, resident directory,
transparency, role-switcher or presentation/report file is changed. Gabriela’s
future local reporting state is not connected to this independent fixture list.
A shared issue store can be agreed when integrating the team’s screens.

The manager’s assignment supplied on 21 September is newer than the explicitly
latest OpenProject snapshot (`2026-09-16-1613`), which still defers tickets to
semester work. A later OpenProject screenshot supplied by Daniel identifies
user story #95, “Administrator Incident Triage & Resolution Workflow.” That
screenshot does not include a full description or acceptance criteria. This
branch provides its frontend presentation demo; it does not claim the full
story or backend issue workflow is complete.

## Repository findings at implementation time

- React 19 + TypeScript + Vite, with no routing library or shared business-data
  store. `App.tsx` switches on `window.location.pathname` for login, password
  change and dashboard; navigation uses full page loads.
- Login, password change and sign-out use localStorage mock accounts and sessions.
  Login’s Google button has no handler. The welcome dashboard was the only
  post-login screen. None of the five new presentation modules was implemented.
- The .NET backend has real CSRF/login/logout endpoints and PostgreSQL sessions,
  but no issues controller/API. Frontend authentication is not wired to it.
- EF Core entities, configurations, migrations and model snapshot are the schema
  authority: users have a global role and optional apartment; buildings have an
  admin; issues reference a building and reporter and have attachments. There
  are no associations, memberships or issue comments.
- `docs/data-model.md` and the migration-generated `docs/schema.sql` have already
  been updated to the implemented eleven-table model. Older planning, API and
  use-case documents still describe the association-based target. Their contracts
  are not evidence that those features exist. Existing prototype S-13/S-15 and
  the React dashboard informed the visual design; association controls and
  unimplemented comments were not copied.
- Docker/nginx configuration supports SPA fallback and the documented HTTPS
  environment. This feature also runs directly in Vite for an offline demo.

## Model boundary

`issueModel.ts` uses the backend numeric status values: Open=1, InProgress=2,
Resolved=3. The presentation label for 3 is **Closed**, as requested. Priorities
match Low=1, Medium=2, High=3, Critical=4. Changes populate `updatedAt` and closure
populates `resolvedAt`. Sequential transitions match the useful part of UC-06;
there is no skip, reopen, assignment or comment implementation. Priority edits
change `updatedAt` locally. Category and note controls were removed because
neither field exists in the implemented Issue model.

Fixture IDs are local demo identifiers, not API UUIDs. `reference`, `reporterName`,
`apartment`, `location` and attachment `alt` are display metadata, not
new database columns. A future
API integration needs real DTOs/UUIDs and server role/building checks.

## Validation

```sh
npm run lint --prefix frontend
npm run build --prefix frontend
node --experimental-strip-types --test frontend/src/features/issue-triage/issueModel.test.mjs
```

The model tests cover fixture enum consistency, independent sequential changes,
timestamps, immutable fixtures, stale actions and terminal closed state.

Browser checked at desktop and 360px: mock administrator login, all five cards,
photo rendering, both transitions, counts, closed state, search with no results,
priority filtering, sorting, priority edits,
clear filters, refresh reset, Escape and backdrop dismissal, focus on the close
button after resolution and focus restoration to the moved card. No browser
console errors or warnings were observed. Backend tests are not part of this
frontend-only change.

## Photo attribution

`assets/broken-water-meter.jpg` is the Wikimedia-generated 960px derivative of
[Broken water meter shows measurement in residential area](https://commons.wikimedia.org/wiki/File:Broken_water_meter_shows_measurement_in_residential_area.jpg)
by Shixart1985, licensed [CC BY 2.0](https://creativecommons.org/licenses/by/2.0/).
Downloaded on 21 September 2026; no further image edits. It is bundled locally
for offline display, with source and license links in the detail dialog. The
photo is illustrative and is not evidence of an actual report at this building.
