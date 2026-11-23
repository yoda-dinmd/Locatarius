import { render, screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import App from "./App";

type Identity = {
  id: string;
  name: string;
  email: string;
  role: "admin" | "resident";
  mustChangePassword: boolean;
};

const admin: Identity = {
  id: "00000000-0000-4000-8000-000000000001",
  name: "Local Admin",
  email: "admin@example.test",
  role: "admin",
  mustChangePassword: false,
};

const resident: Identity = {
  id: "00000000-0000-4000-8000-000000000002",
  name: "Ana Rusu",
  email: "ana@example.test",
  role: "resident",
  mustChangePassword: false,
};

const fetchMock = vi.fn<typeof fetch>();

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

const unauthenticated = () =>
  json(401, {
    error: {
      code: "UNAUTHENTICATED",
      message: "Authentication required.",
      fields: {},
    },
  });

/** Mocks the network boundary: `/api/auth/me` plus pages' own requests. */
function mockBackend(me: () => Response | Promise<Response>) {
  fetchMock.mockImplementation(async (input) => {
    const path = String(input);
    if (path === "/api/auth/me") return me();
    if (path === "/api/residents") return json(200, []);
    throw new Error(`Unexpected request: ${path}`);
  });
}

function renderAt(path: string) {
  window.history.replaceState(null, "", path);
  return render(<App />);
}

function mainNavigation() {
  return screen.getByRole("navigation", { name: "Main navigation" });
}

beforeEach(() => {
  fetchMock.mockReset();
  vi.stubGlobal("fetch", fetchMock);
});

describe("App identity bootstrap", () => {
  it("shows only a loading state while the identity is resolving", async () => {
    let resolveMe: (response: Response) => void = () => {};
    mockBackend(
      () => new Promise<Response>((resolve) => (resolveMe = resolve)),
    );

    renderAt("/dashboard");

    expect(screen.getByRole("status")).toHaveTextContent("Loading Locatarius");
    expect(screen.queryByRole("navigation")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /sign out/i })).not.toBeInTheDocument();
    expect(screen.queryByText(/welcome,/i)).not.toBeInTheDocument();

    resolveMe(json(200, admin));
    expect(
      await screen.findByRole("heading", { name: "Welcome, Local Admin" }),
    ).toBeInTheDocument();
  });

  it("asks for the identity with same-origin credentials", async () => {
    mockBackend(unauthenticated);

    renderAt("/");
    await screen.findByRole("heading", { name: "Welcome Back" });

    expect(fetchMock).toHaveBeenCalledWith(
      "/api/auth/me",
      expect.objectContaining({ credentials: "same-origin" }),
    );
  });

  it("offers a retry when the identity request fails", async () => {
    mockBackend(() => json(500, {}));

    renderAt("/dashboard");

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("Unable to connect to Locatarius");
    expect(
      within(alert).getByRole("button", { name: "Try again" }),
    ).toBeInTheDocument();
    expect(screen.queryByRole("navigation")).not.toBeInTheDocument();
  });
});

describe("App routing", () => {
  it.each(["/", "/dashboard", "/residents", "/admin/issues", "/issues"])(
    "renders the login page for an unauthenticated user at %s",
    async (path) => {
      mockBackend(unauthenticated);

      renderAt(path);

      expect(
        await screen.findByRole("heading", { name: "Welcome Back" }),
      ).toBeInTheDocument();
      expect(screen.queryByRole("navigation")).not.toBeInTheDocument();
    },
  );

  it.each(["/", "/dashboard", "/residents", "/change-password"])(
    "forces the password change for mustChangePassword at %s",
    async (path) => {
      mockBackend(() => json(200, { ...resident, mustChangePassword: true }));

      renderAt(path);

      expect(
        await screen.findByRole("heading", { name: /choose your own\s*password/i }),
      ).toBeInTheDocument();
      expect(screen.getByLabelText("New password")).toBeInTheDocument();
      expect(screen.getByLabelText("Confirm new password")).toBeInTheDocument();
      expect(screen.queryByRole("navigation")).not.toBeInTheDocument();
    },
  );

  it("shows the resident dashboard without the Residents link", async () => {
    mockBackend(() => json(200, resident));

    renderAt("/dashboard");

    expect(
      await screen.findByRole("heading", { name: "Welcome, Ana Rusu" }),
    ).toBeInTheDocument();
    const nav = mainNavigation();
    expect(within(nav).getByRole("link", { name: "Issues" })).toHaveAttribute(
      "href",
      "/issues",
    );
    expect(
      within(nav).queryByRole("link", { name: "Residents" }),
    ).not.toBeInTheDocument();
  });

  it.each(["/residents", "/admin/issues", "/unknown"])(
    "redirects a resident from %s to the dashboard",
    async (path) => {
      mockBackend(() => json(200, resident));

      renderAt(path);

      expect(
        await screen.findByRole("heading", { name: "Welcome, Ana Rusu" }),
      ).toBeInTheDocument();
      expect(window.location.pathname).toBe("/dashboard");
      expect(fetchMock).not.toHaveBeenCalledWith(
        "/api/residents",
        expect.anything(),
      );
    },
  );

  it("shows the admin dashboard with the Residents link", async () => {
    mockBackend(() => json(200, admin));

    renderAt("/dashboard");

    expect(
      await screen.findByRole("heading", { name: "Welcome, Local Admin" }),
    ).toBeInTheDocument();
    const nav = mainNavigation();
    expect(within(nav).getByRole("link", { name: "Residents" })).toHaveAttribute(
      "href",
      "/residents",
    );
    expect(within(nav).getByRole("link", { name: "Issues" })).toHaveAttribute(
      "href",
      "/admin/issues",
    );
  });

  it("lets an admin open the residents directory", async () => {
    mockBackend(() => json(200, admin));

    renderAt("/residents");

    expect(
      await screen.findByRole("heading", { name: "Locatari" }),
    ).toBeInTheDocument();
    expect(window.location.pathname).toBe("/residents");
  });

  it("keeps browser storage empty for an authenticated session", async () => {
    mockBackend(() => json(200, admin));

    renderAt("/dashboard");
    await screen.findByRole("heading", { name: "Welcome, Local Admin" });

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });
});
