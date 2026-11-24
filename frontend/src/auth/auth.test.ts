import { beforeEach, describe, expect, it, vi } from "vitest";

type Route = (init: RequestInit | undefined) => Response;

const fetchMock = vi.fn<typeof fetch>();

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function apiError(
  status: number,
  code: string,
  message: string,
  fields: Record<string, string[]> = {},
): Response {
  return json(status, { error: { code, message, fields } });
}

/** Routes fetch calls by path; each path may answer differently per call. */
function routeFetch(routes: Record<string, Route | Route[]>) {
  const counters = new Map<string, number>();

  fetchMock.mockImplementation(async (input, init) => {
    const path = String(input);
    const route = routes[path];
    if (!route) {
      throw new Error(`Unexpected request: ${path}`);
    }

    const count = counters.get(path) ?? 0;
    counters.set(path, count + 1);
    const handler = Array.isArray(route)
      ? route[Math.min(count, route.length - 1)]
      : route;
    return handler(init);
  });
}

let tokenCounter = 0;
const csrfRoute: Route = () =>
  json(200, { token: `csrf-token-${++tokenCounter}` });

function callsTo(path: string) {
  return fetchMock.mock.calls.filter(([input]) => String(input) === path);
}

function headersOf(init: RequestInit | undefined): Record<string, string> {
  return (init?.headers ?? {}) as Record<string, string>;
}

// The client caches the CSRF token in module memory, so every test loads
// fresh copies of the modules to start without a token.
async function loadModules() {
  vi.resetModules();
  const auth = await import("./auth");
  const client = await import("../api/client");
  return { ...auth, ...client };
}

beforeEach(() => {
  tokenCounter = 0;
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

describe("CSRF handling", () => {
  it("fetches a CSRF token before login and attaches it as X-CSRF-TOKEN", async () => {
    const { login } = await loadModules();
    routeFetch({
      "/api/auth/csrf": csrfRoute,
      "/api/auth/login": () => json(200, { next: "app" }),
    });

    await expect(
      login("admin@example.test", "TemporaryPassword123!"),
    ).resolves.toEqual({ next: "app" });

    expect(fetchMock).toHaveBeenNthCalledWith(1, "/api/auth/csrf", {
      credentials: "same-origin",
      headers: { Accept: "application/json" },
    });
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      "/api/auth/login",
      expect.objectContaining({
        method: "POST",
        credentials: "same-origin",
        headers: expect.objectContaining({
          "Content-Type": "application/json",
          "X-CSRF-TOKEN": "csrf-token-1",
        }),
        body: JSON.stringify({
          email: "admin@example.test",
          password: "TemporaryPassword123!",
        }),
      }),
    );
  });

  it("reuses the in-memory token for later unsafe requests", async () => {
    const { apiRequest } = await loadModules();
    routeFetch({
      "/api/auth/csrf": csrfRoute,
      "/api/residents": () => json(201, { id: "r-1" }),
    });

    await apiRequest("/api/residents", { method: "POST", body: { a: 1 } });
    await apiRequest("/api/residents", { method: "PUT", body: { a: 2 } });

    expect(callsTo("/api/auth/csrf")).toHaveLength(1);
    for (const [, init] of callsTo("/api/residents")) {
      expect(headersOf(init)["X-CSRF-TOKEN"]).toBe("csrf-token-1");
    }
  });

  it("shares one CSRF request between concurrent unsafe requests", async () => {
    const { apiRequest } = await loadModules();
    routeFetch({
      "/api/auth/csrf": csrfRoute,
      "/api/residents": () => json(200, {}),
    });

    await Promise.all([
      apiRequest("/api/residents", { method: "POST", body: {} }),
      apiRequest("/api/residents", { method: "DELETE" }),
    ]);

    expect(callsTo("/api/auth/csrf")).toHaveLength(1);
  });

  it("does not fetch or send a CSRF token for GET requests", async () => {
    const { getIdentity } = await loadModules();
    routeFetch({
      "/api/auth/me": () => apiError(401, "UNAUTHENTICATED", "Authentication required."),
    });

    await getIdentity();

    expect(callsTo("/api/auth/csrf")).toHaveLength(0);
    const [, init] = callsTo("/api/auth/me")[0];
    expect(init?.credentials).toBe("same-origin");
    expect(headersOf(init)).not.toHaveProperty("X-CSRF-TOKEN");
    expect(headersOf(init)).not.toHaveProperty("Content-Type");
  });

  it("refreshes the token and retries once on 403 CSRF_INVALID", async () => {
    const { login } = await loadModules();
    routeFetch({
      "/api/auth/csrf": csrfRoute,
      "/api/auth/login": [
        () => apiError(403, "CSRF_INVALID", "Refresh the page and try again."),
        () => json(200, { next: "change_password" }),
      ],
    });

    await expect(login("ana@example.test", "secret")).resolves.toEqual({
      next: "change_password",
    });

    const loginCalls = callsTo("/api/auth/login");
    expect(loginCalls).toHaveLength(2);
    expect(headersOf(loginCalls[0][1])["X-CSRF-TOKEN"]).toBe("csrf-token-1");
    expect(headersOf(loginCalls[1][1])["X-CSRF-TOKEN"]).toBe("csrf-token-2");
  });

  it("gives up after a single retry when CSRF keeps failing", async () => {
    const { login, ApiError } = await loadModules();
    routeFetch({
      "/api/auth/csrf": csrfRoute,
      "/api/auth/login": () =>
        apiError(403, "CSRF_INVALID", "Refresh the page and try again."),
    });

    const error = await login("ana@example.test", "secret").catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 403, code: "CSRF_INVALID" });
    expect(callsTo("/api/auth/login")).toHaveLength(2);
    expect(callsTo("/api/auth/csrf")).toHaveLength(2);
  });
});

describe("ApiError decoding", () => {
  it("decodes 400 VALIDATION_FAILED with field errors", async () => {
    const { login, ApiError } = await loadModules();
    routeFetch({
      "/api/auth/csrf": csrfRoute,
      "/api/auth/login": () =>
        apiError(400, "VALIDATION_FAILED", "Validation failed.", {
          email: ["Enter a valid email address."],
          password: ["This field is required."],
        }),
    });

    const error = await login("bad", "").catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({
      status: 400,
      code: "VALIDATION_FAILED",
      message: "Validation failed.",
      fields: {
        email: ["Enter a valid email address."],
        password: ["This field is required."],
      },
    });
  });

  it("decodes 401 INVALID_CREDENTIALS and 429 RATE_LIMITED", async () => {
    const { login } = await loadModules();
    routeFetch({
      "/api/auth/csrf": csrfRoute,
      "/api/auth/login": [
        () => apiError(401, "INVALID_CREDENTIALS", "Email or password is incorrect."),
        () => apiError(429, "RATE_LIMITED", "Too many requests. Try again later."),
      ],
    });

    await expect(login("a@example.test", "wrong")).rejects.toMatchObject({
      status: 401,
      code: "INVALID_CREDENTIALS",
    });
    await expect(login("a@example.test", "wrong")).rejects.toMatchObject({
      status: 429,
      code: "RATE_LIMITED",
      message: "Too many requests. Try again later.",
    });
  });

  it("falls back to a generic error for non-JSON failures", async () => {
    const { getIdentity, ApiError } = await loadModules();
    routeFetch({
      "/api/auth/me": () => new Response("Bad gateway", { status: 502 }),
    });

    const error = await getIdentity().catch((e) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 502, code: "HTTP_502", fields: {} });
  });
});

describe("auth functions", () => {
  const identity = {
    id: "8f0f2a4e-0000-4000-8000-000000000001",
    name: "Local Admin",
    email: "admin@example.test",
    role: "admin",
    mustChangePassword: false,
  };

  it("getIdentity returns the server identity, or null on 401", async () => {
    const { getIdentity, getCurrentIdentity } = await loadModules();
    routeFetch({
      "/api/auth/me": [
        () => json(200, identity),
        () => apiError(401, "UNAUTHENTICATED", "Authentication required."),
      ],
    });

    await expect(getIdentity()).resolves.toEqual(identity);
    expect(getCurrentIdentity()).toEqual(identity);

    await expect(getIdentity()).resolves.toBeNull();
    expect(getCurrentIdentity()).toBeNull();
  });

  it("getIdentity rejects a malformed identity", async () => {
    const { getIdentity } = await loadModules();
    routeFetch({
      "/api/auth/me": () => json(200, { ...identity, role: "superuser" }),
    });

    await expect(getIdentity()).rejects.toMatchObject({
      code: "INVALID_IDENTITY",
    });
  });

  it("changePassword sends only newPassword and confirmPassword", async () => {
    const { changePassword } = await loadModules();
    routeFetch({
      "/api/auth/csrf": csrfRoute,
      "/api/auth/change-password": () => json(200, { next: "app" }),
    });

    await expect(
      changePassword("a-long-new-password", "a-long-new-password"),
    ).resolves.toEqual({ next: "app" });

    const [, init] = callsTo("/api/auth/change-password")[0];
    expect(JSON.parse(String(init?.body))).toEqual({
      newPassword: "a-long-new-password",
      confirmPassword: "a-long-new-password",
    });
    expect(headersOf(init)["X-CSRF-TOKEN"]).toBe("csrf-token-1");
  });

  it("signOut posts {} with the CSRF header and clears the identity", async () => {
    const { getIdentity, getCurrentIdentity, signOut } = await loadModules();
    routeFetch({
      "/api/auth/me": () => json(200, identity),
      "/api/auth/csrf": csrfRoute,
      "/api/auth/logout": () => json(200, { message: "Signed out." }),
    });

    await getIdentity();
    await signOut();

    const [, init] = callsTo("/api/auth/logout")[0];
    expect(init).toMatchObject({
      method: "POST",
      credentials: "same-origin",
      body: "{}",
    });
    expect(headersOf(init)["X-CSRF-TOKEN"]).toBe("csrf-token-1");
    expect(getCurrentIdentity()).toBeNull();
  });

  it("signOut treats an already expired session as signed out", async () => {
    const { signOut } = await loadModules();
    routeFetch({
      "/api/auth/csrf": csrfRoute,
      "/api/auth/logout": () =>
        apiError(401, "UNAUTHENTICATED", "Authentication required."),
    });

    await expect(signOut()).resolves.toBeUndefined();
  });

  it("forgets the CSRF token after login so the next request fetches a new one", async () => {
    const { login, signOut } = await loadModules();
    routeFetch({
      "/api/auth/csrf": csrfRoute,
      "/api/auth/login": () => json(200, { next: "app" }),
      "/api/auth/logout": () => json(200, {}),
    });

    await login("admin@example.test", "secret");
    await signOut();

    expect(callsTo("/api/auth/csrf")).toHaveLength(2);
    const [, logoutInit] = callsTo("/api/auth/logout")[0];
    expect(headersOf(logoutInit)["X-CSRF-TOKEN"]).toBe("csrf-token-2");
  });
});

describe("browser storage", () => {
  it("keeps localStorage and sessionStorage empty through a full session", async () => {
    const { login, changePassword, getIdentity, signOut } = await loadModules();
    routeFetch({
      "/api/auth/csrf": csrfRoute,
      "/api/auth/login": () => json(200, { next: "change_password" }),
      "/api/auth/change-password": () => json(200, { next: "app" }),
      "/api/auth/me": () =>
        json(200, {
          id: "u-1",
          name: "Ana Rusu",
          email: "ana@example.test",
          role: "resident",
          mustChangePassword: false,
        }),
      "/api/auth/logout": () => json(200, {}),
    });

    await login("ana@example.test", "TemporaryPassword123!");
    await changePassword("a-brand-new-password", "a-brand-new-password");
    await getIdentity();
    await signOut();

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
    expect(document.cookie).toBe("");
  });
});
