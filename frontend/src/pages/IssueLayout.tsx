import type { ReactNode } from "react";
import type { SessionUser } from "../auth/auth";
import { signOut } from "../auth/auth";
import "../styles/Dashboard.css";

type IssueLayoutProps = {
  user: SessionUser;
  activePage: "dashboard" | "issues";
  children: ReactNode;
};

function getInitials(name: string) {
  return name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}

export default function IssueLayout({ user, activePage, children }: IssueLayoutProps) {
  const issuesHref = user.role === "admin" ? "/admin/issues" : "/issues";

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
          <button className="sign-out-button" type="button" onClick={handleSignOut}>
            Sign out
          </button>
        </div>
      </header>

      <div className="dashboard-layout">
        <aside className="dashboard-sidebar">
          <nav aria-label="Main navigation">
            <a className={`dashboard-nav-link ${activePage === "dashboard" ? "active" : ""}`} href="/dashboard">
              Dashboard
            </a>
            <a
              className={`dashboard-nav-link ${activePage === "issues" ? "active" : ""}`}
              href={issuesHref}
              aria-current={activePage === "issues" ? "page" : undefined}
            >
              Issues
            </a>
          </nav>
        </aside>
        <section className="dashboard-content issue-page-content">{children}</section>
      </div>
    </main>
  );
}
