import { useCallback, useState } from "react";
import type { SubmitEvent } from "react";
import type { SessionUser } from "../auth/auth";
import { describeIssueError, getIssue, updateIssue } from "../auth/issues";
import { useAsyncResource } from "../hooks/useAsyncResource";
import IssueLayout from "./IssueLayout";
import "../styles/IssueFormPage.css";

type CloseIssuePageProps = { user: SessionUser; issueId: string };

/**
 * Administrator confirmation that marks an issue Resolved (status 3) through
 * PATCH /api/issues/{id}. The API has no fields for fix notes or fix photos
 * yet, so none are collected rather than silently discarding them.
 */
export default function CloseIssuePage({ user, issueId }: CloseIssuePageProps) {
  const [error, setError] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const isAdmin = user.role === "admin";
  // Residents never trigger a request: the page is admin-only.
  const load = useCallback(
    (signal: AbortSignal) => isAdmin ? getIssue(issueId, { signal }) : Promise.resolve(null),
    [isAdmin, issueId],
  );
  const { resource, reload } = useAsyncResource(load);

  if (resource.status === "loading") {
    return <IssueLayout user={user} activePage="issues"><div className="issue-form-page" aria-busy="true"><p role="status">Loading issue…</p></div></IssueLayout>;
  }

  if (resource.status === "error") {
    return (
      <IssueLayout user={user} activePage="issues">
        <div className="issue-form-page">
          <a className="back-link" href={`/issues/${issueId}`}>⮜ Return to issue</a>
          <div className="issues-alert issues-alert-error" role="alert">{describeIssueError(resource.error)}</div>
          <button className="secondary-action" type="button" onClick={reload}>Try again</button>
        </div>
      </IssueLayout>
    );
  }

  const issue = resource.data;
  if (!issue || !isAdmin) {
    return <IssueLayout user={user} activePage="issues"><div className="issue-form-page"><h1>Issue not found</h1><a className="back-link" href="/issues">Back to issues</a></div></IssueLayout>;
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSaving) return;
    setIsSaving(true);
    setError("");
    try {
      await updateIssue(issueId, { status: 3 });
      window.location.href = `/issues/${issueId}`;
    } catch (caught) {
      setError(describeIssueError(caught));
      setIsSaving(false);
    }
  }

  return (
    <IssueLayout user={user} activePage="issues">
      <div className="issue-form-page">
        <a className="back-link" href={`/issues/${issueId}`}>⮜ Return to issue</a>
        <p className="issues-kicker">Close issue</p>
        <h1>{issue.title}</h1>
        {issue.status === 3 ? (
          <p className="muted-copy">This issue is already closed.</p>
        ) : (
          <form className="issue-form standalone-form" onSubmit={handleSubmit}>
            {error && <div className="issues-alert issues-alert-error" role="alert">{error}</div>}
            <p>Mark this issue as closed? The resident will see it as Closed. You can reopen it later from the issues board.</p>
            <div className="issue-form-actions"><a className="secondary-action button-link" href={`/issues/${issueId}`}>Cancel</a><button className="primary-action" type="submit" disabled={isSaving}>{isSaving ? "Closing..." : "Close issue"}</button></div>
          </form>
        )}
      </div>
    </IssueLayout>
  );
}
