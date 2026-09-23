import { defineConfig, devices } from "@playwright/test";

// Set NAVIGATION_BASE_URL to check a running container instead of starting Vite.
const externalURL = process.env.NAVIGATION_BASE_URL;

export default defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  use: {
    baseURL: externalURL ?? "http://127.0.0.1:4173",
    channel: process.env.PLAYWRIGHT_CHANNEL,
    trace: "retain-on-failure",
  },
  projects: [
    { name: "desktop", use: { ...devices["Desktop Chrome"] } },
    {
      name: "tablet",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 768, height: 900 },
      },
    },
    {
      name: "mobile",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 390, height: 844 },
      },
    },
  ],
  webServer: externalURL
    ? undefined
    : {
        command: "npm run dev -- --host 127.0.0.1 --port 4173 --strictPort",
        url: "http://127.0.0.1:4173",
        reuseExistingServer: false,
      },
});
