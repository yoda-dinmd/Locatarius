import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiErrorResponse as apiError, issueDto, jsonResponse as json } from "../test/issueFixtures";

const fetchMock = vi.fn<typeof fetch>();
const ISSUE_ID = issueDto().issueId;

function calls(path: string) {
  return fetchMock.mock.calls.filter(([input]) => String(input) === path);
}

function headersOf(init: RequestInit | undefined): Record<string, string> {
  return (init?.headers ?? {}) as Record<string, string>;
}

// The API client caches its CSRF token in module memory; load fresh modules per test.
async function loadModules() {
  vi.resetModules();
  const issues = await import("./issues");
  const client = await import("../api/client");
  return { ...issues, ApiError: client.ApiError };
}

function route(handlers: Record<string, (init: RequestInit | undefined) => Response>) {
  fetchMock.mockImplementation(async (input, init) => {
    const handler = handlers[String(input)];
    if (!handler) throw new Error(`Unexpected request: ${String(input)}`);
    return handler(init);
  });
}

const csrf = () => json(200, { token: "csrf-1" });

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

describe("listIssues", () => {
  it("GETs /api/issues with the session cookie and no CSRF token", async () => {
    const { listIssues } = await loadModules();
    route({ "/api/issues": () => json(200, [issueDto(), issueDto({ issueId: "x", status: 3, resolvedAt: "2026-11-21T00:00:00Z" })]) });

    const issues = await listIssues();

    expect(issues.map((issue) => issue.issueId)).toEqual([ISSUE_ID, "x"]);
    const [, init] = calls("/api/issues")[0];
    expect(init?.credentials).toBe("same-origin");
    expect(init?.method).toBe("GET");
    expect(headersOf(init)).not.toHaveProperty("X-CSRF-TOKEN");
    expect(calls("/api/auth/csrf")).toHaveLength(0);
  });

  it("passes the abort signal through to fetch", async () => {
    const { listIssues } = await loadModules();
    route({ "/api/issues": () => json(200, []) });
    const controller = new AbortController();

    await listIssues({ signal: controller.signal });

    expect(calls("/api/issues")[0][1]?.signal).toBe(controller.signal);
  });

  it.each([
    ["a non-array payload", { issues: [] }],
    ["an unknown status", [issueDto({ status: 4 })]],
    ["a missing reporter", [issueDto({ reporter: null })]],
    ["a string priority", [issueDto({ priority: "3" })]],
  ])("rejects %s as INVALID_ISSUE", async (_, payload) => {
    const { listIssues } = await loadModules();
    route({ "/api/issues": () => json(200, payload) });

    await expect(listIssues()).rejects.toMatchObject({ code: "INVALID_ISSUE" });
  });

  it("surfaces 401 as an ApiError", async () => {
    const { listIssues, ApiError } = await loadModules();
    route({ "/api/issues": () => apiError(401, "UNAUTHENTICATED", "Authentication required.") });

    const error = await listIssues().catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 401, code: "UNAUTHENTICATED" });
  });
});

describe("getIssue", () => {
  it("returns the issue for a GUID id", async () => {
    const { getIssue } = await loadModules();
    route({ [`/api/issues/${ISSUE_ID}`]: () => json(200, issueDto()) });

    await expect(getIssue(ISSUE_ID)).resolves.toMatchObject({ issueId: ISSUE_ID, buildingNumber: "A" });
  });

  it("returns null on 404 ISSUE_NOT_FOUND (missing or another building)", async () => {
    const { getIssue } = await loadModules();
    route({ [`/api/issues/${ISSUE_ID}`]: () => apiError(404, "ISSUE_NOT_FOUND", "Issue not found.") });

    await expect(getIssue(ISSUE_ID)).resolves.toBeNull();
  });

  it("returns null without a request for non-GUID ids", async () => {
    const { getIssue } = await loadModules();

    await expect(getIssue("ISS-1001")).resolves.toBeNull();
    await expect(getIssue("../auth/me")).resolves.toBeNull();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("rethrows other failures", async () => {
    const { getIssue } = await loadModules();
    route({ [`/api/issues/${ISSUE_ID}`]: () => new Response("down", { status: 503 }) });

    await expect(getIssue(ISSUE_ID)).rejects.toMatchObject({ status: 503 });
  });
});

describe("createIssue", () => {
  it("POSTs only title and description with the CSRF header", async () => {
    const { createIssue } = await loadModules();
    route({ "/api/auth/csrf": csrf, "/api/issues": () => json(201, issueDto()) });

    await createIssue({ title: "Broken entrance light", description: "The entrance light does not turn on." });

    const [, init] = calls("/api/issues")[0];
    expect(init?.method).toBe("POST");
    expect(headersOf(init)["X-CSRF-TOKEN"]).toBe("csrf-1");
    expect(headersOf(init)["Content-Type"]).toBe("application/json");
    expect(JSON.parse(String(init?.body))).toEqual({
      title: "Broken entrance light",
      description: "The entrance light does not turn on.",
    });
  });

  it("rejects with field errors on 400 VALIDATION_FAILED", async () => {
    const { createIssue } = await loadModules();
    route({
      "/api/auth/csrf": csrf,
      "/api/issues": () => apiError(400, "VALIDATION_FAILED", "Validation failed.", { title: ["Use between 5 and 120 characters."] }),
    });

    await expect(createIssue({ title: "Hi", description: "Long enough text" })).rejects.toMatchObject({
      code: "VALIDATION_FAILED",
      fields: { title: ["Use between 5 and 120 characters."] },
    });
  });
});

describe("updateIssue", () => {
  it("PATCHes only the provided fields with the CSRF header", async () => {
    const { updateIssue } = await loadModules();
    route({ "/api/auth/csrf": csrf, [`/api/issues/${ISSUE_ID}`]: () => json(200, issueDto({ status: 2 })) });

    const saved = await updateIssue(ISSUE_ID, { status: 2 });

    expect(saved.status).toBe(2);
    const [, init] = calls(`/api/issues/${ISSUE_ID}`)[0];
    expect(init?.method).toBe("PATCH");
    expect(headersOf(init)["X-CSRF-TOKEN"]).toBe("csrf-1");
    expect(JSON.parse(String(init?.body))).toEqual({ status: 2 });
  });

  it("rejects with ISSUE_RESOLVED when changing priority of a closed issue", async () => {
    const { updateIssue } = await loadModules();
    route({
      "/api/auth/csrf": csrf,
      [`/api/issues/${ISSUE_ID}`]: () => apiError(409, "ISSUE_RESOLVED", "Reopen the issue before changing its priority."),
    });

    await expect(updateIssue(ISSUE_ID, { priority: 1 })).rejects.toMatchObject({ status: 409, code: "ISSUE_RESOLVED" });
  });
});

describe("describeIssueError", () => {
  it("maps known codes, keeps server messages and hides 5xx details", async () => {
    const { describeIssueError, ApiError } = await loadModules();

    expect(describeIssueError(new ApiError(401, "UNAUTHENTICATED", "x"))).toMatch(/session has expired/);
    expect(describeIssueError(new ApiError(404, "APARTMENT_NOT_FOUND", "x"))).toMatch(/not assigned to an apartment/);
    expect(describeIssueError(new ApiError(409, "ISSUE_RESOLVED", "Reopen the issue before changing its priority.")))
      .toBe("Reopen the issue before changing its priority.");
    expect(describeIssueError(new ApiError(500, "INTERNAL_ERROR", "stack trace here"))).not.toMatch(/stack/);
    expect(describeIssueError(new TypeError("Failed to fetch"))).toMatch(/Unable to reach/);
  });
});

describe("browser storage", () => {
  it("never writes issues to localStorage or sessionStorage", async () => {
    const { listIssues, createIssue, updateIssue } = await loadModules();
    route({
      "/api/auth/csrf": csrf,
      "/api/issues": (init) => json(init?.method === "POST" ? 201 : 200, init?.method === "POST" ? issueDto() : [issueDto()]),
      [`/api/issues/${ISSUE_ID}`]: () => json(200, issueDto({ status: 2 })),
    });

    await listIssues();
    await createIssue({ title: "Broken entrance light", description: "The entrance light does not turn on." });
    await updateIssue(ISSUE_ID, { status: 2 });

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });
});
