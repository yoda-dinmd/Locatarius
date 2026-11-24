import { useCallback, useState } from "react";
import type { SessionUser } from "../auth/auth";
import { describeIssueError, getIssue, updateIssue } from "../auth/issues";
import type { Issue, IssueStatus } from "../auth/issues";
import { priorityLabels, reporterName, statusLabels } from "../features/issue-triage/issueModel";
import { useAsyncResource } from "../hooks/useAsyncResource";
import IssueLayout from "./IssueLayout";
import "../styles/IssueFormPage.css";

type IssueDetailsPageProps = { user: SessionUser; issueId: string };

function Evidence({ attachments }: { attachments: Issue["attachments"] }) {
  if (attachments.length === 0) return <p className="muted-copy">No evidence attached.</p>;

  return <div className="detail-media-list">{attachments.map((file) => (
    file.fileType.startsWith("image/")
      ? <img className="issue-media" src={file.fileUrl} alt="Evidence attached to this issue" key={file.attachmentId} />
      : file.fileType.startsWith("video/")
        ? <video className="issue-media" src={file.fileUrl} controls key={file.attachmentId} />
        : <a className="issue-evidence" href={file.fileUrl} target="_blank" rel="noreferrer" key={file.attachmentId}>{file.fileUrl.split("/").pop() || "Attachment"}</a>
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

function NotFound({ user, message }: { user: SessionUser; message?: string }) {
  return (
    <IssueLayout user={user} activePage="issues">
      <div className="issue-form-page">
        <h1>Issue not found</h1>
        {message && <p className="muted-copy">{message}</p>}
        <a className="back-link" href="/issues">Back to issues</a>
      </div>
    </IssueLayout>
  );
}

export default function IssueDetailsPage({ user, issueId }: IssueDetailsPageProps) {
  const [isStatusMenuOpen, setIsStatusMenuOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [saveError, setSaveError] = useState("");
  const load = useCallback((signal: AbortSignal) => getIssue(issueId, { signal }), [issueId]);
  const { resource, reload, update } = useAsyncResource(load);

  if (resource.status === "loading") {
    return (
      <IssueLayout user={user} activePage="issues">
        <div className="issue-form-page" aria-busy="true"><p role="status">Loading issue…</p></div>
      </IssueLayout>
    );
  }

  if (resource.status === "error") {
    return (
      <IssueLayout user={user} activePage="issues">
        <div className="issue-form-page">
          <a className="back-link" href="/issues">⮜ Back to issues</a>
          <div className="issues-alert issues-alert-error" role="alert">{describeIssueError(resource.error)}</div>
          <button className="secondary-action" type="button" onClick={reload}>Try again</button>
        </div>
      </IssueLayout>
    );
  }

  const issue = resource.data;
  if (!issue) return <NotFound user={user} />;
  const status = statusLabels[issue.status];

  async function handleStatusChange(nextStatus: IssueStatus) {
    setIsStatusMenuOpen(false);
    if (!issue || nextStatus === issue.status || isSaving) return;
    setIsSaving(true);
    setSaveError("");
    try {
      const saved = await updateIssue(issue.issueId, { status: nextStatus });
      update(() => saved);
    } catch (error) {
      setSaveError(describeIssueError(error));
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <IssueLayout user={user} activePage="issues">
      <div className="issue-form-page issue-details-page">
        <a className="back-link" href="/issues"><span aria-hidden="true">⮜</span>Back to issues</a>
        {saveError && <div className="issues-alert issues-alert-error" role="alert">{saveError}</div>}
        <div className="detail-heading">
          <div>
            <p className="issues-kicker">Building {issue.buildingNumber}</p>
            <h1>{issue.title}</h1>
            <div className="detail-summary">
              <div className="detail-summary-row"><span className="detail-summary-label">Reported by:</span><span>{reporterName(issue)}{issue.reporter.apartment && ` · Apartment ${issue.reporter.apartment.apartmentNumber}`}</span></div>
              <div className="detail-summary-row"><span className="detail-summary-label">Created:</span><span>{formatDateTime(issue.createdAt)}</span></div>
              {issue.updatedAt && <div className="detail-summary-row"><span className="detail-summary-label">Updated:</span><span>{formatDateTime(issue.updatedAt)}</span></div>}
              {issue.resolvedAt && <div className="detail-summary-row"><span className="detail-summary-label">Closed:</span><span>{formatDateTime(issue.resolvedAt)}</span></div>}
              <div className="detail-summary-row"><span className="detail-summary-label">Priority:</span><span>{priorityLabels[issue.priority]}</span></div>
              {user.role === "admin" && issue.status !== 3 ? (
                <label className="detail-status-field">
                  <span className="detail-summary-label">Status:</span>
                  <span className="status-dropdown">
                    <button className="status-dropdown-trigger" type="button" aria-haspopup="listbox" aria-expanded={isStatusMenuOpen} disabled={isSaving} onClick={() => setIsStatusMenuOpen((current) => !current)}>
                      {isSaving ? "Saving…" : status}<span aria-hidden="true">⮟</span>
                    </button>
                    {isStatusMenuOpen && <span className="status-dropdown-menu" role="listbox" aria-label="Issue status">
                      {([1, 2] as const).map((value) => <button className="status-dropdown-option" type="button" role="option" aria-selected={issue.status === value} key={value} onClick={() => void handleStatusChange(value)}>{statusLabels[value]}</button>)}
                    </span>}
                  </span>
                </label>
              ) : (
                <div className="detail-summary-row"><span className="detail-summary-label">Status:</span><span className={`status-badge status-${status.toLowerCase().replace(/\s+/g, "-")}`}>{status}</span></div>
              )}
            </div>
          </div>
        </div>
        <div className="detail-grid">
          <section className="detail-section"><h2>Issue description</h2><p>{issue.description}</p></section>
          <section className="detail-section"><h2>Reported evidence</h2><Evidence attachments={issue.attachments} /></section>
        </div>
        {user.role === "admin" && issue.status === 2 && (
          <div className="detail-actions">
            <a className="primary-action button-link" href={`/issues/${issue.issueId}/close`}>Close issue</a>
          </div>
        )}
      </div>
    </IssueLayout>
  );
}
