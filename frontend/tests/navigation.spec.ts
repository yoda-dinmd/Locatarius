import { expect, test, type Page } from "@playwright/test";

type Role = "admin" | "resident";

/**
 * Isolated mock session: the app reads identity from `GET /api/auth/me`
 * (HttpOnly cookie session), so tests answer that request instead of seeding
 * browser storage. No real users or shared data are touched.
 */
async function mockSession(page: Page, role: Role) {
  // Registered first, so it only handles API calls the routes below don't.
  // Match the path prefix, not a glob: "**/api/**" would also catch the
  // app's own source module /src/api/client.ts in the Vite dev server.
  await page.route((url) => url.pathname.startsWith("/api/"), (route) =>
    route.fulfill({
      status: 404,
      json: { error: { code: "NOT_FOUND", message: "Not mocked.", fields: {} } },
    }),
  );
  await page.route((url) => url.pathname === "/api/auth/me", (route) =>
    route.fulfill({
      json: {
        id:
          role === "admin"
            ? "00000000-0000-4000-8000-000000000001"
            : "00000000-0000-4000-8000-000000000002",
        name: "Navigation Test",
        email: "navigation@locatarius.test",
        role,
        mustChangePassword: false,
      },
    }),
  );
}

async function expectNavigation(
  page: Page,
  active: string,
  issuesPath: string,
) {
  const nav = page.getByRole("navigation", { name: "Main navigation" });
  await expect(nav).toBeVisible();
  const sidebar = page.getByRole("complementary", {
    name: "Association and navigation",
  });
  // The sidebar holds only navigation and a decorative footer; association
  // details were removed from it in the September layout change.
  await expect(sidebar).toBeVisible();
  await expect(sidebar.getByRole("navigation")).toHaveCount(1);
  const footer = sidebar.locator(".app-sidebar-footer");
  if (page.viewportSize()!.width > 720) {
    await expect(footer).toBeInViewport({ ratio: 1 });
  } else {
    await expect(footer).toBeHidden();
    // Mobile nav is a wrapping row (the admin's fourth link may wrap): links
    // must sit side by side and stay inside the viewport without page scroll.
    const layout = await nav.getByRole("link").evaluateAll((links) => ({
      tops: links.map((link) => link.getBoundingClientRect().top),
      overflow: links.some((link) => {
        const box = link.getBoundingClientRect();
        return box.left < 0 || box.right > window.innerWidth;
      }),
      pageScrollsSideways:
        document.documentElement.scrollWidth > window.innerWidth,
    }));
    expect(new Set(layout.tops).size).toBeLessThan(layout.tops.length);
    expect(layout.overflow).toBe(false);
    expect(layout.pageScrollsSideways).toBe(false);
  }
  for (const [name, href] of [
    ["Dashboard", "/dashboard"],
    ["Transparency", "/transparency"],
    ["Issues", issuesPath],
  ]) {
    const link = nav.getByRole("link", { name, exact: true });
    await expect(link).toBeVisible();
    await expect(link).toHaveAttribute("href", href);
  }
  await expect(nav.locator('[aria-current="page"]')).toHaveCount(1);
  await expect(
    nav.getByRole("link", { name: active, exact: true }),
  ).toHaveAttribute("aria-current", "page");
}

for (const role of ["resident", "admin"] as const) {
  test(`${role} can navigate between all main pages and return from issue subpages`, async ({
    page,
  }) => {
    const errors: string[] = [];
    page.on("pageerror", (error) => errors.push(error.message));
    await mockSession(page, role);
    const issuesPath = role === "admin" ? "/admin/issues" : "/issues";
    await page.goto("/transparency");
    await expectNavigation(page, "Transparency", issuesPath);
    const nav = page.getByRole("navigation", { name: "Main navigation" });
    await nav.getByRole("link", { name: "Issues", exact: true }).click();
    await expect(page).toHaveURL(new RegExp(`${issuesPath}$`));
    await expectNavigation(page, "Issues", issuesPath);
    await nav.getByRole("link", { name: "Transparency", exact: true }).click();
    await expect(page).toHaveURL(/\/transparency$/);
    await expectNavigation(page, "Transparency", issuesPath);
    await nav.getByRole("link", { name: "Dashboard", exact: true }).click();
    await expectNavigation(page, "Dashboard", issuesPath);
    await nav.getByRole("link", { name: "Transparency", exact: true }).click();
    await expectNavigation(page, "Transparency", issuesPath);
    await page.goBack();
    await expectNavigation(page, "Dashboard", issuesPath);
    const paths =
      role === "resident"
        ? ["/issues", "/issues/new", "/issues/ISS-1001", "/issues/missing"]
        : [
            "/admin/issues",
            "/issues",
            "/issues/ISS-1001",
            "/issues/ISS-1001/close",
            "/issues/missing/close",
          ];
    for (const path of paths) {
      await page.goto(path);
      await expectNavigation(page, "Issues", issuesPath);
      // Keyboard navigation must work too, not just pointer clicks.
      await nav
        .getByRole("link", { name: "Transparency", exact: true })
        .focus();
      await page.keyboard.press("Enter");
      await expect(page).toHaveURL(/\/transparency$/);
      await expectNavigation(page, "Transparency", issuesPath);
    }
    await page.reload();
    await expectNavigation(page, "Transparency", issuesPath);
    expect(errors).toEqual([]);
  });
}

test("sidebar placement does not depend on page length", async ({
  page,
}, testInfo) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await mockSession(page, "resident");
  let previousWidth: number | undefined;
  for (const path of ["/dashboard", "/issues", "/transparency"]) {
    await page.goto(path);
    const sidebar = page.getByRole("complementary", {
      name: "Association and navigation",
    });
    const box = await sidebar.boundingBox();
    expect(box).not.toBeNull();
    if (previousWidth !== undefined) expect(box!.width).toBe(previousWidth);
    previousWidth = box!.width;
    if (page.viewportSize()!.width > 720) {
      const footer = sidebar.locator(".app-sidebar-footer");
      await expect(footer).toBeInViewport({ ratio: 1 });
      await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
      await expect(footer).toBeInViewport({ ratio: 1 });
    }
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.screenshot({
      path: testInfo.outputPath(`${path.slice(1)}.png`),
    });
  }
});

test("transparency keeps correct values when the reporting month changes", async ({
  page,
}) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  // /transparency requires a signed-in user.
  await mockSession(page, "resident");
  await page.goto("/transparency");
  await expect(page.locator(".tp-big-number")).toContainText("41,850");
  await expect(page.locator(".tp-average")).toContainText("38,125 MDL");
  await expect(page.locator(".tp-change-total")).toContainText("−2,650 MDL");
  await page.getByRole("button", { name: "March 2026: 34,300 MDL" }).focus();
  await expect(page.locator(".tp-trend-readout")).toContainText("34,300 MDL");
  await page.locator("summary").first().focus();
  await page.keyboard.press("Enter");
  await expect(page.locator("details").first()).toHaveAttribute("open", "");
  // The month control is a button that opens a listbox of month options.
  const monthTrigger = page.getByRole("button", { name: "Expense month" });
  await expect(monthTrigger).toHaveText(/August 2026/);
  const monthMenu = page.getByRole("listbox");
  await expect(monthMenu).toBeHidden();
  await monthTrigger.click();
  await expect(monthMenu).toBeVisible();
  await monthMenu.getByRole("option", { name: "July 2026" }).click();
  await expect(monthMenu).toBeHidden();
  await expect(monthTrigger).toHaveText(/July 2026/);
  await expect(page).toHaveURL(/[?&]month=july\b/);
  await expect(page.locator(".tp-big-number")).toContainText("44,500");
  await expect(page.locator(".tp-average")).toContainText("37,380 MDL");
  await expect(page.locator(".tp-change-total")).toContainText("+4,800 MDL");
  await page.reload();
  await expect(monthTrigger).toHaveText(/July 2026/);
  await expect(page.locator(".tp-big-number")).toContainText("44,500");
  await expect(page.locator(".tp-column-bar").first()).toHaveCSS(
    "animation-name",
    "none",
  );
});
