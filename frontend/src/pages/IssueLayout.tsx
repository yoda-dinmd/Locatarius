import AppSidebar from "../components/AppSidebar";
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
        <AppSidebar activePage={activePage} role={user.role} />
        <section className="dashboard-content issue-page-content">{children}</section>
      </div>
    </main>
  );
}
