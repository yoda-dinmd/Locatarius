import { getPendingUser, getSession } from "./auth/auth";
import AdminIssuesPage from "./pages/AdminIssuesPage";
import ChangePassword from "./pages/ChangePassword";
import Dashboard from "./pages/Dashboard";
import IssuesPage from "./pages/IssuesPage";
import LoginPage from "./pages/LoginPage";
import IssueDetailsPage from "./pages/IssueDetailsPage";
import ReportIssuePage from "./pages/ReportIssuePage";
import CloseIssuePage from "./pages/CloseIssuePage";

export default function App() {
  const path = window.location.pathname;
  const session = getSession();
  const pendingUser = getPendingUser();

  if (path === "/change-password" && pendingUser) {
    return <ChangePassword />;
  }

  if (path === "/dashboard" && session) {
    return <Dashboard user={session} />;
  }

  if (path === "/admin/issues" && session?.role === "admin") {
    return <AdminIssuesPage user={session} />;
  }

  if (path === "/issues" && session) {
    return <IssuesPage user={session} />;
  }

  const closeIssueMatch = path.match(/^\/issues\/([^/]+)\/close$/);
  if (closeIssueMatch && session) {
    return <CloseIssuePage user={session} issueId={closeIssueMatch[1]} />;
  }

  if (path === "/issues/new" && session && session.role === "resident") {
    return <ReportIssuePage user={session} />;
  }

  const issueMatch = path.match(/^\/issues\/([^/]+)$/);
  if (issueMatch && session) {
    return <IssueDetailsPage user={session} issueId={issueMatch[1]} />;
  }

  return <LoginPage />;
}
