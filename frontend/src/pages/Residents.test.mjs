import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { after, before, test } from "node:test";
import { fileURLToPath } from "node:url";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { createServer } from "vite";

const projectRoot = new URL("../../", import.meta.url);
let vite;

before(async () => {
  vite = await createServer({
    root: fileURLToPath(projectRoot),
    appType: "custom",
    logLevel: "silent",
    server: { hmr: false, middlewareMode: true },
  });
});

after(async () => {
  await vite.close();
});

async function read(relativePath) {
  return readFile(new URL(relativePath, projectRoot), "utf8");
}

test("resident mock data contains a valid Romanian directory", async () => {
  const residents = JSON.parse(await read("src/data/residentsMock.json"));

  assert.ok(residents.length >= 6 && residents.length <= 10);
  assert.equal(new Set(residents.map(({ id }) => id)).size, residents.length);

  for (const resident of residents) {
    assert.equal(typeof resident.id, "number");
    assert.match(resident.name, /^[A-ZĂÂÎȘȚ][\p{L}-]+(?: [A-ZĂÂÎȘȚ][\p{L}-]+)+$/u);
    assert.match(resident.apartment, /^[A-Z]?-?\d+[A-Z]?$/);
    assert.ok(["Proprietar", "Chiriaș"].includes(resident.role));
    assert.ok(["Activ", "Inactiv"].includes(resident.status));
  }
});

test("resident page renders the required table and accessible actions", async () => {
  const { default: Residents } = await vite.ssrLoadModule(
    "/src/pages/Residents.tsx",
  );
  const html = renderToStaticMarkup(
    React.createElement(Residents, {
      user: {
        id: "admin-1",
        name: "Victor Munteanu",
        email: "admin@locatarius.test",
        role: "admin",
      },
    }),
  );

  for (const label of [
    "Nume",
    "Apartament",
    "Rol",
    "Status",
    "Acțiuni",
    "Adaugă locatar",
    "Editează",
    "Șterge",
  ]) {
    assert.match(html, new RegExp(label));
  }

  assert.match(html, /8 locatari înregistrați/);
  assert.match(html, /aria-label="Editează Ana Popescu"/);
  assert.match(html, /aria-label="Șterge Vlad Bălan"/);
});

test("resident page renders its empty state without a table", async () => {
  const { default: Residents } = await vite.ssrLoadModule(
    "/src/pages/Residents.tsx",
  );
  const html = renderToStaticMarkup(
    React.createElement(Residents, {
      user: {
        id: "admin-1",
        name: "Victor Munteanu",
        email: "admin@locatarius.test",
        role: "admin",
      },
      residents: [],
    }),
  );

  assert.match(html, /Nu există locatari/);
  assert.match(html, /Adaugă primul locatar/);
  assert.doesNotMatch(html, /<table/);
});

test("resident route renders the directory only for an admin session", async () => {
  const { default: App } = await vite.ssrLoadModule("/src/App.tsx");
  const storage = new Map();

  globalThis.localStorage = {
    getItem: (key) => storage.get(key) ?? null,
    removeItem: (key) => storage.delete(key),
    setItem: (key, value) => storage.set(key, value),
  };
  globalThis.window = {
    location: { pathname: "/residents", href: "/residents" },
  };

  storage.set(
    "locatarius-mock-session",
    JSON.stringify({
      id: "admin-1",
      name: "Victor Munteanu",
      email: "admin@locatarius.test",
      role: "admin",
    }),
  );
  const adminHtml = renderToStaticMarkup(React.createElement(App));

  storage.set(
    "locatarius-mock-session",
    JSON.stringify({
      id: "resident-1",
      name: "Ana Popescu",
      email: "ana@locatarius.test",
      role: "resident",
    }),
  );
  const residentHtml = renderToStaticMarkup(React.createElement(App));

  assert.match(adminHtml, /Lista locatarilor/);
  assert.match(adminHtml, /href="\/residents"/);
  assert.doesNotMatch(residentHtml, /Lista locatarilor/);
  assert.doesNotMatch(residentHtml, /href="\/residents"/);
  assert.match(residentHtml, /Welcome, Ana Popescu/);
});

test("resident table is responsive and uses only the existing color palette", async () => {
  const [residentsCss, ...existingCss] = await Promise.all([
    read("src/styles/Residents.css"),
    read("src/styles/Global.css"),
    read("src/styles/Dashboard.css"),
    read("src/styles/LoginPage.css"),
    read("src/styles/ChangePassword.css"),
  ]);

  assert.match(residentsCss, /overflow-x:\s*auto/);
  assert.match(residentsCss, /:hover/);
  assert.match(residentsCss, /:focus-visible/);
  assert.match(residentsCss, /@media\s*\(max-width:/);

  const existingColors = new Set(
    existingCss.flatMap((css) => css.match(/#[0-9a-f]{3,8}/gi) ?? []),
  );
  const residentColors = residentsCss.match(/#[0-9a-f]{3,8}/gi) ?? [];

  assert.deepEqual(
    [...new Set(residentColors.filter((color) => !existingColors.has(color)))],
    [],
  );
});
