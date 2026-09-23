# Frontend structure and navigation

- `src/pages/`: route-level page components and the existing Issues layout.
- `src/styles/`: page styles.
- `src/components/`: UI shared across pages. `MainNavigation.tsx` owns the main
  links, active-page styling and role-specific Issues destination.
- `src/features/`: domain-specific components, fixtures and helpers.
- `src/App.tsx`: route selection and existing session/role checks. Links use normal
  browser navigation; no extra router library is needed for this change.

Dashboard and Transparency use `AppSidebar`; all Issues screens use it through
`IssueLayout`, including detail, creation, closing and not-found states.
`AppSidebar` owns the shared demo association identity and sidebar
footer, styled by `styles/AppSidebar.css`. It renders `MainNavigation` for the links.
The desktop sidebar fits the viewport and stays visible on long pages; on mobile,
association details and links remain visible and the decorative footer is hidden.
Sidebar sizing is shared, not overridden by individual pages.
Add future main-navigation entries in `MainNavigation`, not in individual pages.
Navigation visibility is not authorization: access checks remain in the route/API.

## Navigation regression checks

```sh
cd frontend
npm ci
npx playwright install chromium
npm run test:navigation
```

The test command starts its own Vite server on port 4173. Tests cover resident and
admin navigation at desktop/tablet/mobile widths, returning from Issues subpages,
active links, keyboard activation, reload and browser back. They also verify footer
visibility while scrolling, consistent sidebar sizing, month totals and reduced motion. They use isolated mock sessions.
Playwright is a development-only dependency; it adds no browser application code.

To test the built preview container instead:

```sh
NAVIGATION_BASE_URL=http://localhost:8081 npm run test:navigation
```

If using an already-installed Chrome instead of Playwright Chromium, add
`PLAYWRIGHT_CHANNEL=chrome` to the test command.

## Container updates

The Docker frontend serves a build copied into its image. Editing source or running
`npm run build` locally does not update a running container. For the standalone
port-8081 preview, rebuild and recreate it using the instructions in
[src/features/transparency/README.md](src/features/transparency/README.md).
For the configured Compose frontend use `docker compose up -d --build frontend`
from the repository root. Use the URL/port of that deployment when reviewing it.

## Release scope

This is a mock frontend implementation, not a live financial service. Existing
login/session behavior uses browser-local mock state; the association label,
expenses and announcements are synthetic. Before live deployment, integrate
server-authenticated association context and authorized financial/notice APIs.
The public `/transparency` route intentionally exposes only demo data. It must be
protected before real association data is introduced. This change does not modify
the existing authentication model or claim to deliver those backend integrations.
