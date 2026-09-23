import Icon from "./Icon";
import MainNavigation, { type MainNavigationProps } from "./MainNavigation";
import "../styles/AppSidebar.css";

// Association identity is shared demo content until association context is integrated.
export default function AppSidebar({ activePage, role }: MainNavigationProps) {
  return (
    <aside
      className="dashboard-sidebar app-sidebar"
      aria-label="Association and navigation"
    >
      <div className="app-association">
        <p className="app-association-label">Your association</p>
        <div className="app-association-details">
          <span className="app-association-icon">
            <Icon name="home" size={24} />
          </span>
          <div>
            <strong>Teilor Residence</strong>
            <small>12 Teilor Street · Demo</small>
          </div>
        </div>
      </div>
      <MainNavigation activePage={activePage} role={role} />
      <div className="app-sidebar-footer">
        <span>A shared home.</span>
        <p>A clearer picture.</p>
        <small>Your building’s finances and updates, in one place.</small>
      </div>
    </aside>
  );
}
