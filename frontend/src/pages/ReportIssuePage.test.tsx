import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { SessionUser } from "../auth/auth";
import { apiErrorResponse, issueDto, jsonResponse } from "../test/issueFixtures";
import ReportIssuePage from "./ReportIssuePage";

const fetchMock = vi.fn<typeof fetch>();
const resident: SessionUser = {
  id: "20000000-0000-0000-0000-000000000001",
  name: "Ana Rusu",
  email: "ana@example.test",
  role: "resident",
  mustChangePassword: false,
};

function issuePosts() {
  return fetchMock.mock.calls.filter(([input, init]) => String(input) === "/api/issues" && init?.method === "POST");
}

function fill(title: string, description: string) {
  fireEvent.change(screen.getByLabelText(/Title/), { target: { value: title } });
  fireEvent.change(screen.getByLabelText(/Description/), { target: { value: description } });
  fireEvent.click(screen.getByRole("button", { name: "Submit issue" }));
}

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

describe("ReportIssuePage", () => {
  it("has no file upload and POSTs only the trimmed title and description", async () => {
    fetchMock.mockImplementation(async (input) => String(input) === "/api/auth/csrf"
      ? jsonResponse(200, { token: "csrf-1" })
      : jsonResponse(201, issueDto()));
    render(<ReportIssuePage user={resident} />);
    expect(document.querySelector('input[type="file"]')).toBeNull();

    fill("  Broken entrance light  ", "  The entrance light does not turn on.  ");

    await waitFor(() => expect(issuePosts()).toHaveLength(1));
    expect(JSON.parse(String(issuePosts()[0][1]?.body))).toEqual({
      title: "Broken entrance light",
      description: "The entrance light does not turn on.",
    });
  });

  it("blocks obviously invalid input without calling the API", () => {
    render(<ReportIssuePage user={resident} />);

    fill("Hi", "short");

    expect(screen.getAllByText("Use between 5 and 120 characters.")).toHaveLength(1);
    expect(screen.getByText("Use between 10 and 2000 characters.")).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("shows server field errors and the unassigned-apartment message", async () => {
    let attempt = 0;
    fetchMock.mockImplementation(async (input) => {
      if (String(input) === "/api/auth/csrf") return jsonResponse(200, { token: "csrf-1" });
      return ++attempt === 1
        ? apiErrorResponse(400, "VALIDATION_FAILED", "Validation failed.", { title: ["Remove control characters."] })
        : apiErrorResponse(404, "APARTMENT_NOT_FOUND", "No apartment is assigned to your account.");
    });
    render(<ReportIssuePage user={resident} />);

    fill("Valid title", "A long enough description.");
    expect(await screen.findByText("Remove control characters.")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Submit issue" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("not assigned to an apartment");
  });
});
