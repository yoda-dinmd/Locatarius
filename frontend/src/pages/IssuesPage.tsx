import { useMemo, useState } from "react";
import type { SessionUser } from "../auth/auth";
import { getAllIssues, getIssuesForResident, type IssueFilter } from "../auth/issues";
import IssueLayout from "./IssueLayout";
import "../styles/IssuesPage.css";

type IssuesPageProps = { user: SessionUser };
type ResidentFilter = "All" | "My issues" | IssueFilter;

const residentFilters: ResidentFilter[] = ["All", "My issues", "Open", "In Progress", "Closed"];
const adminFilters: ResidentFilter[] = ["All", "Open", "In Progress", "Closed"];

export default function IssuesPage({ user }: IssuesPageProps) {
  const [filter, setFilter] = useState<ResidentFilter>("All");
  const allVisibleIssues = user.role === "admin" ? getAllIssues() : getIssuesForResident(user.id);
  const visibleIssues = useMemo(() => {
    if (filter === "All") return allVisibleIssues;
    if (filter === "My issues") return allVisibleIssues.filter((issue) => issue.residentId === user.id);
    return allVisibleIssues.filter((issue) => issue.status === filter);
  }, [allVisibleIssues, filter, user.id]);

  return (
    <IssueLayout user={user} activePage="issues">
      <div className="issues-toolbar">
        <div>
          <p className="issues-kicker">Issue reports</p>
          <h1>Building issues</h1>
        </div>
        {user.role === "resident" && <a className="primary-action button-link" href="/issues/new">Report Issue ✚</a>}
      </div>

      <div className="issues-filters" aria-label="Filter issues">
        {(user.role === "admin" ? adminFilters : residentFilters).map((option) => (
          <button key={option} type="button" className={filter === option ? "filter-button active" : "filter-button"} onClick={() => setFilter(option)}>{option}</button>
        ))}
      </div>

      <div className="issue-list">
        {visibleIssues.length === 0 ? <div className="issue-empty-state">No issues match the selected filter.</div> : visibleIssues.map((issue) => (
          <a className="issue-card issue-card-link" href={`/issues/${issue.id}`} key={issue.id}>
            <div className="issue-card-header">
              <div>
                <p className="issue-meta">{issue.buildingId} · Reported by {issue.reporterName} · {new Date(issue.createdAt).toLocaleDateString()}</p>
                <h3>{issue.title}</h3>
              </div>
              <span className={`status-badge status-${issue.status.toLowerCase().replace(/\s+/g, "-")}`}>{issue.status}</span>
            </div>
            <p className="issue-description">{issue.description}</p>
            <div className={issue.evidenceName ? "issue-evidence" : "issue-evidence muted"}>{issue.evidenceName ? `Evidence: ${issue.evidenceName}` : "No evidence attached."}</div>
          </a>
        ))}
      </div>
    </IssueLayout>
  );
}
