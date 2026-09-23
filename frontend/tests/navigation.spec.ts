import { expect, test, type Page } from "@playwright/test";

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
  await expect(
    sidebar.getByText("Your association", { exact: true }),
  ).toBeVisible();
  await expect(
    sidebar.getByText("Teilor Residence", { exact: true }),
  ).toBeVisible();
  await expect(
    sidebar.getByText("12 Teilor Street · Demo", { exact: true }),
  ).toBeVisible();
  const footer = sidebar.locator(".app-sidebar-footer");
  if (page.viewportSize()!.width > 720) {
    await expect(footer).toBeInViewport({ ratio: 1 });
  } else {
    await expect(footer).toBeHidden();
    const positions = await nav
      .getByRole("link")
      .evaluateAll((links) =>
        links.map((link) => link.getBoundingClientRect().top),
      );
    expect(new Set(positions).size).toBe(1);
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
    // Isolated mock session: navigation tests do not change real users or shared storage.
    await page.addInitScript((sessionRole) => {
      localStorage.setItem(
        "locatarius-mock-session",
        JSON.stringify({
          id: sessionRole === "admin" ? "admin-1" : "resident-1",
          name: "Navigation Test",
          email: "navigation@locatarius.test",
          role: sessionRole,
          buildingId: "BLD-01",
        }),
      );
    }, role);
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
  await page.addInitScript(() =>
    localStorage.setItem(
      "locatarius-mock-session",
      JSON.stringify({
        id: "resident-1",
        name: "Navigation Test",
        email: "navigation@locatarius.test",
        role: "resident",
        buildingId: "BLD-01",
      }),
    ),
  );
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
  await page.goto("/transparency");
  await expect(page.locator(".tp-big-number")).toContainText("41,850");
  await expect(page.locator(".tp-average")).toContainText("38,125 MDL");
  await expect(page.locator(".tp-change-total")).toContainText("−2,650 MDL");
  await page.getByRole("button", { name: "March 2026: 34,300 MDL" }).focus();
  await expect(page.locator(".tp-trend-readout")).toContainText("34,300 MDL");
  await page.locator("summary").first().focus();
  await page.keyboard.press("Enter");
  await expect(page.locator("details").first()).toHaveAttribute("open", "");
  await page.getByLabel("Expense month").selectOption("4");
  await expect(page.locator(".tp-big-number")).toContainText("44,500");
  await expect(page.locator(".tp-average")).toContainText("37,380 MDL");
  await expect(page.locator(".tp-change-total")).toContainText("+4,800 MDL");
  await page.reload();
  await expect(page.getByLabel("Expense month")).toHaveValue("4");
  await expect(page.locator(".tp-column-bar").first()).toHaveCSS(
    "animation-name",
    "none",
  );
});
