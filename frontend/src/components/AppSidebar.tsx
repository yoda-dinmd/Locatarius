import MainNavigation, { type MainNavigationProps } from "./MainNavigation";
import "../styles/AppSidebar.css";

// Association identity is shared demo content until association context is integrated.
export default function AppSidebar({ activePage, role }: MainNavigationProps) {
  return (
    <aside
      className="dashboard-sidebar app-sidebar"
      aria-label="Association and navigation"
    >
      <MainNavigation activePage={activePage} role={role} />
      <div className="app-sidebar-footer">
        <p>A clearer picture.</p>
        <small>Your building’s finances and updates, in one place.</small>
      </div>
    </aside>
  );
}
