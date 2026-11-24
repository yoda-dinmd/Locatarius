import { useMemo, useState } from "react";
import type { SessionUser } from "../auth/auth";
import { describeIssueError, listIssues } from "../auth/issues";
import { reporterName, statusLabels } from "../features/issue-triage/issueModel";
import { useAsyncResource } from "../hooks/useAsyncResource";
import IssueLayout from "./IssueLayout";
import "../styles/IssuesPage.css";

type IssuesPageProps = { user: SessionUser };
type IssueFilter = "All" | "My issues" | (typeof statusLabels)[keyof typeof statusLabels];

const residentFilters: IssueFilter[] = ["All", "My issues", "Open", "In Progress", "Closed"];
const adminFilters: IssueFilter[] = ["All", "Open", "In Progress", "Closed"];

function loadIssues(signal: AbortSignal) {
  return listIssues({ signal });
}

export default function IssuesPage({ user }: IssuesPageProps) {
  const [filter, setFilter] = useState<IssueFilter>("All");
  const { resource, reload } = useAsyncResource(loadIssues);

  const visibleIssues = useMemo(() => {
    if (resource.status !== "ready") return [];
    const issues = resource.data;
    if (filter === "All") return issues;
    if (filter === "My issues") return issues.filter((issue) => issue.reportedBy === user.id);
    return issues.filter((issue) => statusLabels[issue.status] === filter);
  }, [resource, filter, user.id]);

  return (
    <IssueLayout user={user} activePage="issues">
      <div className="issues-toolbar">
        <div>
          <h1>Building issues</h1>
        </div>
        {user.role === "resident" && <a className="primary-action button-link" href="/issues/new">Report Issue ✚</a>}
      </div>

      <div className="issues-filters" aria-label="Filter issues">
        {(user.role === "admin" ? adminFilters : residentFilters).map((option) => (
          <button key={option} type="button" className={filter === option ? "filter-button active" : "filter-button"} aria-pressed={filter === option} onClick={() => setFilter(option)}>{option}</button>
        ))}
      </div>

      <div className="issue-list" aria-busy={resource.status === "loading"}>
        {resource.status === "loading" && <div className="issue-empty-state" role="status">Loading issues…</div>}
        {resource.status === "error" && (
          <div className="issues-alert issues-alert-error" role="alert">
            {describeIssueError(resource.error)}{" "}
            <button type="button" className="filter-button" onClick={reload}>Try again</button>
          </div>
        )}
        {resource.status === "ready" && (visibleIssues.length === 0
          ? <div className="issue-empty-state">{resource.data.length === 0 ? "No issues have been reported yet." : "No issues match the selected filter."}</div>
          : visibleIssues.map((issue) => {
            const label = statusLabels[issue.status];
            return (
              <a className="issue-card issue-card-link" href={`/issues/${issue.issueId}`} key={issue.issueId}>
                <div className="issue-card-header">
                  <div>
                    <p className="issue-meta">Building {issue.buildingNumber} · Reported by {reporterName(issue)} · {new Date(issue.createdAt).toLocaleDateString()}</p>
                    <h3>{issue.title}</h3>
                  </div>
                  <span className={`status-badge status-${label.toLowerCase().replace(/\s+/g, "-")}`}>{label}</span>
                </div>
                <p className="issue-description">{issue.description}</p>
                <div className={issue.attachments.length ? "issue-evidence" : "issue-evidence muted"}>
                  {issue.attachments.length ? `Evidence: ${issue.attachments.length} file${issue.attachments.length === 1 ? "" : "s"}` : "No evidence attached."}
                </div>
              </a>
            );
          }))}
      </div>
    </IssueLayout>
  );
}
