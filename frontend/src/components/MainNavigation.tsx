import Icon from "./Icon";
import type { UserRole } from "../auth/auth";

type Page = "dashboard" | "transparency" | "issues";

export type MainNavigationProps = {
  activePage: Page;
  role?: UserRole;
};

export default function MainNavigation({
  activePage,
  role,
}: MainNavigationProps) {
  const links: {
    page: Page;
    label: string;
    href: string;
    icon: "home" | "board" | "issue";
  }[] = [
    { page: "dashboard", label: "Dashboard", href: "/dashboard", icon: "home" },
    {
      page: "transparency",
      label: "Transparency",
      href: "/transparency",
      icon: "board",
    },
    {
      page: "issues",
      icon: "issue",
      label: "Issues",
      href: role === "admin" ? "/admin/issues" : "/issues",
    },
  ];

  return (
    <nav aria-label="Main navigation">
      {links.map(({ page, label, href, icon }) => (
        <a
          key={page}
          className={`dashboard-nav-link${activePage === page ? " active" : ""}`}
          href={href}
          aria-current={activePage === page ? "page" : undefined}
        >
          <Icon name={icon} />
          {label}
        </a>
      ))}
    </nav>
  );
}
