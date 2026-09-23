import type { SessionUser } from "../auth/auth";
import { signOut } from "../auth/auth";
import "../styles/Dashboard.css";

type DashboardProps = {
  user: SessionUser;
};

function getInitials(name: string) {
  return name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .slice(0, 2)
    .toUpperCase();
}

export default function Dashboard({ user }: DashboardProps) {
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
          <nav aria-label="Main navigation">
            <a className="dashboard-nav-link active" href="/dashboard" aria-current="page">
              Dashboard
            </a>
            <a className="dashboard-nav-link" href={user.role === "admin" ? "/admin/issues" : "/issues"}>
              Issues
            </a>
          </nav>
        </aside>

        <section className="dashboard-content dashboard-home-content">
          <h1>Welcome, {user.name}</h1>
          <p className="dashboard-message">
            Your Locatarius dashboard is ready.
          </p>
        </section>
      </div>
    </main>
  );
}
