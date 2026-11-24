import { useCallback, useEffect, useState } from "react";
import { getIdentity } from "./auth/auth";
import type { SessionUser } from "./auth/auth";
import AdminIssuesPage from "./pages/AdminIssuesPage";
import ChangePassword from "./pages/ChangePassword";
import Dashboard from "./pages/Dashboard";
import IssuesPage from "./pages/IssuesPage";
import LoginPage from "./pages/LoginPage";

import TransparencyPage from "./pages/TransparencyPage";
import IssueDetailsPage from "./pages/IssueDetailsPage";
import ReportIssuePage from "./pages/ReportIssuePage";
import CloseIssuePage from "./pages/CloseIssuePage";
import Residents from "./pages/Residents";

type IdentityState =
  | { status: "loading" }
  | { status: "ready"; user: SessionUser | null }
  | { status: "error" };

function renderAuthenticatedRoute(path: string, user: SessionUser) {
  if (/^\/transparency(?:\/version-1)?\/?$/.test(path)) {
    return <TransparencyPage user={user} />;
  }

  if (path === "/dashboard") {
    return <Dashboard user={user} />;
  }

  if (path === "/admin/issues" && user.role === "admin") {
    return <AdminIssuesPage user={user} />;
  }

  if (path === "/issues") {
    return <IssuesPage user={user} />;
  }

  const closeIssueMatch = path.match(/^\/issues\/([^/]+)\/close$/);
  if (closeIssueMatch) {
    return <CloseIssuePage user={user} issueId={closeIssueMatch[1]} />;
  }

  if (path === "/issues/new" && user.role === "resident") {
    return <ReportIssuePage user={user} />;
  }

  const issueMatch = path.match(/^\/issues\/([^/]+)$/);
  if (issueMatch) {
    return <IssueDetailsPage user={user} issueId={issueMatch[1]} />;
  }

  if (path === "/residents" && user.role === "admin") {
    return <Residents user={user} />;
  }

  // Unknown, sign-in or role-restricted paths fall back to the dashboard.
  if (path !== "/dashboard") {
    window.history.replaceState(null, "", "/dashboard");
  }
  return <Dashboard user={user} />;
}

export default function App() {
  const [identity, setIdentity] = useState<IdentityState>({
    status: "loading",
  });

  const loadIdentity = useCallback((isCancelled: () => boolean) => {
    getIdentity()
      .then((user) => {
        if (!isCancelled()) {
          setIdentity({ status: "ready", user });
        }
      })
      .catch(() => {
        if (!isCancelled()) {
          setIdentity({ status: "error" });
        }
      });
  }, []);

  useEffect(() => {
    let cancelled = false;
    loadIdentity(() => cancelled);
    return () => {
      cancelled = true;
    };
  }, [loadIdentity]);

  // Render nothing protected until the server has confirmed the identity.
  if (identity.status === "loading") {
    return (
      <main className="login-page" aria-busy="true">
        <p role="status" aria-live="polite">
          Loading Locatarius…
        </p>
      </main>
    );
  }

  if (identity.status === "error") {
    return (
      <main className="login-page">
        <div role="alert">
          <p>Unable to connect to Locatarius. Check your connection and try again.</p>
          <button
            className="submit-button"
            type="button"
            onClick={() => {
              setIdentity({ status: "loading" });
              loadIdentity(() => false);
            }}
          >
            Try again
          </button>
        </div>
      </main>
    );
  }

  const { user } = identity;
  const path = window.location.pathname;

  if (!user) {
    return <LoginPage />;
  }

  // A temporary-password session may only change the password.
  if (user.mustChangePassword) {
    return <ChangePassword />;
  }

  return renderAuthenticatedRoute(path, user);
}
