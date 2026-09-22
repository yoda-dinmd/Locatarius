import { useState } from "react";
import type { SessionUser } from "../auth/auth";
import { getAllIssues, getIssuesForResident, updateIssueStatus, type IssueRecord, type IssueEvidence } from "../auth/issues";
import IssueLayout from "./IssueLayout";
import "../styles/IssueFormPage.css";

type IssueDetailsPageProps = { user: SessionUser; issueId: string };

function Evidence({ files, name, type, url }: { files?: IssueEvidence[]; name?: string; type?: string; url?: string }) {
  const evidenceFiles = files?.length ? files : name ? [{ name, type: type ?? "", url: url ?? "" }] : [];
  if (evidenceFiles.length === 0) return <p className="muted-copy">No evidence attached.</p>;

  return <div className="detail-media-list">{evidenceFiles.map((file, index) => (
    file.url && file.type.startsWith("image/")
      ? <img className="issue-media" src={file.url} alt={file.name} key={`${file.name}-${index}`} />
      : file.url && file.type.startsWith("video/")
        ? <video className="issue-media" src={file.url} controls key={`${file.name}-${index}`} />
        : <p className="issue-evidence" key={`${file.name}-${index}`}>{file.name}</p>
  ))}</div>;
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString([], {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function getReporterName(issue: IssueRecord) {
  if (issue.reporterName) {
    return issue.reporterName;
  }

  const reporterNames: Record<string, string> = {
    "resident-1": "Ana Popescu",
    "resident-2": "Ana Munteanu",
    "resident-3": "Ion Munteanu",
  };

  return reporterNames[issue.residentId] ?? "Unknown resident";
}

export default function IssueDetailsPage({ user, issueId }: IssueDetailsPageProps) {
  const [isStatusMenuOpen, setIsStatusMenuOpen] = useState(false);
  const availableIssues = user.role === "admin" ? getAllIssues() : getIssuesForResident(user.id);
  const issue = availableIssues.find((candidate) => candidate.id === issueId);

  if (!issue) {
    return (
      <IssueLayout user={user} activePage="issues">
        <div className="issue-form-page"><h1>Issue not found</h1><a className="back-link" href="/issues">Back to issues</a></div>
      </IssueLayout>
    );
  }

  function handleStatusChange(status: IssueRecord["status"]) {
    updateIssueStatus(issueId, status);
    window.location.reload();
  }

  return (
    <IssueLayout user={user} activePage="issues">
      <div className="issue-form-page issue-details-page">
        <a className="back-link" href="/issues"><span aria-hidden="true">⮜</span>Back to issues</a>
        <div className="detail-heading">
          <div>
            <p className="issues-kicker">{issue.buildingId}</p>
            <h1>{issue.title}</h1>
            <div className="detail-summary">
              <div className="detail-summary-row"><span className="detail-summary-label">Reported by:</span><span>{getReporterName(issue)}</span></div>
              <div className="detail-summary-row"><span className="detail-summary-label">Created:</span><span>{formatDateTime(issue.createdAt)}</span></div>
              <div className="detail-summary-row"><span className="detail-summary-label">Updated:</span><span>{formatDateTime(issue.updatedAt)}</span></div>
              {user.role === "admin" && issue.status !== "Closed" && (
                <label className="detail-status-field">
                  <span className="detail-summary-label">Status:</span>
                  <span className="status-dropdown">
                    <button className="status-dropdown-trigger" type="button" aria-haspopup="listbox" aria-expanded={isStatusMenuOpen} onClick={() => setIsStatusMenuOpen((current) => !current)}>
                      {issue.status}<span aria-hidden="true">⮟</span>
                    </button>
                    {isStatusMenuOpen && <span className="status-dropdown-menu" role="listbox" aria-label="Issue status">
                      {(["Open", "In Progress"] as const).map((status) => <button className="status-dropdown-option" type="button" role="option" aria-selected={issue.status === status} key={status} onClick={() => handleStatusChange(status)}>{status}</button>)}
                    </span>}
                  </span>
                </label>
              )}
              {user.role === "admin" && issue.status === "Closed" && <div className="detail-summary-row"><span className="detail-summary-label">Status:</span><span className="status-badge status-closed">Closed</span></div>}
            </div>
          </div>
        </div>
        <div className="detail-grid">
          <section className="detail-section"><h2>Issue description</h2><p>{issue.description}</p></section>
          <section className="detail-section"><h2>Reported evidence</h2><Evidence files={issue.evidenceFiles} name={issue.evidenceName} type={issue.evidenceType} url={issue.evidenceUrl} /></section>
          {issue.fixDescription && <section className="detail-section"><h2>Admin update</h2><p>{issue.fixDescription}</p><Evidence files={issue.fixEvidenceFiles} name={issue.fixEvidenceName} type={issue.fixEvidenceType} url={issue.fixEvidenceUrl} /></section>}
        </div>
        {user.role === "admin" && issue.status !== "Closed" && (
          <div className="detail-actions">
            {issue.status === "In Progress" && <a className="primary-action button-link" href={`/issues/${issue.id}/close`}>Close issue</a>}
          </div>
        )}
      </div>
    </IssueLayout>
  );
}
