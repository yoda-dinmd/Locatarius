import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: { "/api": "http://localhost:5113" },
  },
  test: {
    clearMocks: true,
    restoreMocks: true,
    unstubGlobals: true,
    environment: "jsdom",
    setupFiles: "./src/test/setup.ts",
    // Only Vitest unit tests; Playwright (tests/*.spec.ts) and node:test
    // (*.test.mjs) suites keep their own runners.
    include: ["src/**/*.test.{ts,tsx}"],
  },
})
