import type { SessionUser } from "../auth/auth";
import { signOut } from "../auth/auth";
import "../styles/Dashboard.css";
import IssueTriage from "../features/issue-triage/IssueTriage";

type DashboardProps = {
  user: SessionUser;
  view?: "dashboard" | "issues";
};

function getInitials(name: string) {
  return name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}

export default function Dashboard({ user, view = "dashboard" }: DashboardProps) {
  function handleSignOut() {
    signOut();
    window.location.href = "/";
  }

  return (
    <main className="dashboard-page">
      <header className="dashboard-header">
        <a className="dashboard-brand" href="/dashboard">
          <span className="brand-mark">L</span>
          <span>Locatarius</span>
        </a>

        <div className="dashboard-user">
          <span className="user-initials">{getInitials(user.name)}</span>

          <span className="user-details">
            <strong>{user.name}</strong>
            <small>{user.role === "admin" ? "Administrator" : "Resident"}</small>
          </span>

          <button
            className="sign-out-button"
            type="button"
            onClick={handleSignOut}
          >
            Sign out
          </button>
        </div>
      </header>

      <div className="dashboard-layout">
        <aside className="dashboard-sidebar">
          <a className="dashboard-logo-text" href="/dashboard">
            Locatarius
          </a>

          <nav aria-label="Main navigation">
            <a className={`dashboard-nav-link${view === "dashboard" ? " active" : ""}`} href="/dashboard" aria-current={view === "dashboard" ? "page" : undefined}>
              Dashboard
            </a>
            {user.role === "admin" && (
              <a className={`dashboard-nav-link${view === "issues" ? " active" : ""}`} href="/admin/issues" aria-current={view === "issues" ? "page" : undefined}>
                Issues
              </a>
            )}
          </nav>
        </aside>

        <section className="dashboard-content">
          {view === "issues" && user.role === "admin" ? (
            <IssueTriage />
          ) : (
            <>
              <p className="dashboard-eyebrow">Dashboard</p>
              <h1>Welcome, {user.name}</h1>
              <p className="dashboard-message">
                Your Locatarius dashboard is ready.
              </p>
            </>
          )}
        </section>
      </div>
    </main>
  );
}
