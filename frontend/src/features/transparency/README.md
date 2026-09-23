# Resident transparency dashboard

The selected Clear & Calm design is now the only implementation. Open
`/transparency`; the original `/transparency/version-1` review URL remains an alias.
Versions 2 and 3, the design switcher, category/notice filters and their unused
styles and data fields have been removed.

The page presents the monthly total, a previous-month comparison, expense category
shares, announcements, monthly spending history, average spending and an itemized
explanation of the monthly change. July and August 2026 are selectable complete
reporting periods. Notices are dated September and do not change with the financial
month. Chart values are available by pointer, touch and keyboard.

All content remains synthetic; no login or backend is required. The documented
product scope currently excludes accounting and notifications, so this requested
frontend does not establish backend support or complete an OpenProject package.
MDL remains an explicit domain assumption. Repair spending is included in expenses;
no repair fund balance, budget or apartment billing data is invented.

## Run in Docker

From the repository root, rebuild after source changes:

```sh
docker build -t locatarius-frontend-preview ./frontend
```

If the previous preview container is running, stop it first:

```sh
docker stop locatarius-frontend-preview
```

Start the rebuilt frontend:

```sh
docker run --rm --name locatarius-frontend-preview -p 127.0.0.1:8081:80 locatarius-frontend-preview
```

Open `http://localhost:8081/transparency`. Stop with Ctrl+C. The container serves a
production build without hot reload. No database or HTTPS setup is needed for this
standalone synthetic page.

For an existing full Compose stack, use `docker compose up -d --build frontend`
and open `https://localhost/transparency` through the running reverse proxy.
First-time full-stack setup is documented in `docs/local-https.md`.

## Implementation and validation

Reuses React/TypeScript, the dashboard shell styling, teal/mint palette, existing
fonts and brand mark. No new dependencies. CSS charts respect reduced motion;
native details/summary elements provide keyboard-accessible announcement expansion.

`src/features/transparency/data.ts` is the single source for totals, shares, comparisons,
monthly trend and average. August: 16,500 + 8,400 + 7,500 + 9,450 = 41,850 MDL.
July: 44,500 MDL. August changes: −4,500 repairs + 200 cleaning + 0 elevator +
1,650 utilities = −2,650 MDL, or 6% lower after rounding. Historical averages
include March through the selected month and are rounded to whole MDL.

Run `npm run build` and `npm run lint` from frontend. Browser review covers
360, 390, 768, 1024 and 1440px, expense-month changes, chart focus, notice expansion,
reduced motion, accessibility scans and browser errors. Automated accessibility
scans do not replace manual screen-reader testing.

The final implementation passed build/lint, arithmetic checks and the above browser
checks against the production Docker container. Desktop/mobile accessibility scans
reported no violations; no browser console errors or horizontal overflow were found.
