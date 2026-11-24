import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiErrorResponse, issueDto, jsonResponse } from "../../test/issueFixtures";
import IssueTriage from "./IssueTriage";

const fetchMock = vi.fn<typeof fetch>();
const ISSUE_ID = issueDto().issueId;
const ISSUE_PATH = `/api/issues/${ISSUE_ID}`;

type Handler = (init: RequestInit | undefined) => Response | Promise<Response>;

function route(handlers: Record<string, Handler>) {
  fetchMock.mockImplementation(async (input, init) => {
    const handler = handlers[String(input)];
    if (!handler) throw new Error(`Unexpected request: ${String(input)}`);
    return handler(init);
  });
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((done) => { resolve = done; });
  return { promise, resolve };
}

function column(label: string) {
  return screen.getByRole("region", { name: label });
}

async function openDetailsAndPickStatus(label: string) {
  fireEvent.click(await screen.findByRole("button", { name: /Broken entrance light/ }));
  fireEvent.click(document.getElementById("triage-detail-status")!);
  fireEvent.click(screen.getByRole("option", { name: label }));
}

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
  // jsdom has no modal dialog support; emulate the open attribute.
  HTMLDialogElement.prototype.showModal = function showModal(this: HTMLDialogElement) { this.setAttribute("open", ""); };
  HTMLDialogElement.prototype.close = function close(this: HTMLDialogElement) { this.removeAttribute("open"); };
});

describe("IssueTriage with the live API", () => {
  it("loads issues from GET /api/issues into their status columns", async () => {
    route({
      "/api/issues": () => jsonResponse(200, [
        issueDto(),
        issueDto({ issueId: "50000000-0000-0000-0000-000000000003", title: "Heating not working", status: 3, resolvedAt: "2026-11-21T08:00:00Z" }),
      ]),
    });

    render(<IssueTriage />);

    expect(screen.getByRole("status")).toHaveTextContent("Loading issues…");
    expect(await within(column("Open")).findByText("Broken entrance light")).toBeInTheDocument();
    expect(within(column("Closed")).getByText("Heating not working")).toBeInTheDocument();
    expect(screen.getByRole("status")).toHaveTextContent("Showing 2 of 2 issues");
  });

  it("moves a card immediately, PATCHes the status and keeps the server copy", async () => {
    const patch = deferred<Response>();
    route({
      "/api/issues": () => jsonResponse(200, [issueDto()]),
      "/api/auth/csrf": () => jsonResponse(200, { token: "csrf-1" }),
      [ISSUE_PATH]: () => patch.promise,
    });
    render(<IssueTriage />);

    await openDetailsAndPickStatus("In Progress");

    // Optimistic: already in the In Progress column while the request is pending.
    expect(within(column("In Progress")).getByText("Broken entrance light")).toBeInTheDocument();
    await waitFor(() => expect(fetchMock.mock.calls.some(([input]) => String(input) === ISSUE_PATH)).toBe(true));
    const [, init] = fetchMock.mock.calls.find(([input]) => String(input) === ISSUE_PATH)!;
    expect(init?.method).toBe("PATCH");
    expect(JSON.parse(String(init?.body))).toEqual({ status: 2 });

    patch.resolve(jsonResponse(200, issueDto({ status: 2, updatedAt: "2026-11-22T09:00:00Z" })));

    expect(await screen.findByRole("status")).toHaveTextContent("moved to In Progress");
    expect(within(column("In Progress")).getByText("Broken entrance light")).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });

  it("rolls the card back and shows an alert when the server rejects the change", async () => {
    const patch = deferred<Response>();
    route({
      "/api/issues": () => jsonResponse(200, [issueDto()]),
      "/api/auth/csrf": () => jsonResponse(200, { token: "csrf-1" }),
      [ISSUE_PATH]: () => patch.promise,
    });
    render(<IssueTriage />);

    await openDetailsAndPickStatus("Closed");
    expect(within(column("Closed")).getByText("Broken entrance light")).toBeInTheDocument();

    patch.resolve(apiErrorResponse(404, "ISSUE_NOT_FOUND", "Issue not found."));

    expect(await screen.findByRole("alert")).toHaveTextContent("was not changed: This issue no longer exists or is not available to you.");
    expect(within(column("Open")).getByText("Broken entrance light")).toBeInTheDocument();
    expect(within(column("Closed")).queryByText("Broken entrance light")).not.toBeInTheDocument();
  });

  it("shows a load error with a working retry", async () => {
    let attempt = 0;
    route({
      "/api/issues": () => (++attempt === 1
        ? new Response("Bad gateway", { status: 502 })
        : jsonResponse(200, [issueDto()])),
    });
    render(<IssueTriage />);

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("The server could not complete the request.");

    fireEvent.click(within(alert).getByRole("button", { name: "Try again" }));

    expect(await within(column("Open")).findByText("Broken entrance light")).toBeInTheDocument();
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
});
