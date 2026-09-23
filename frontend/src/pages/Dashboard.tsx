import type { SessionUser } from "../auth/auth";
import IssueLayout from "./IssueLayout";
import "../styles/Dashboard.css";

type DashboardProps = {
  user: SessionUser;
};

export default function Dashboard({ user }: DashboardProps) {
  return (
    <IssueLayout user={user} activePage="dashboard">
      <section className="dashboard-content dashboard-home-content">
        <h1>Welcome, {user.name}</h1>
        <p className="dashboard-message">Your Locatarius dashboard is ready.</p>
      </section>
    </IssueLayout>
  );
}
