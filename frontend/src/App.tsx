import { getPendingUser, getSession } from "./auth/auth";
import ChangePassword from "./pages/ChangePassword";
import Dashboard from "./pages/Dashboard";
import LoginPage from "./pages/LoginPage";

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
    return <Dashboard user={session} view="issues" />;
  }

  return <LoginPage />;
}
