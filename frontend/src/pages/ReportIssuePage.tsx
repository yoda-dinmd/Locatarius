import { useState } from "react";
import type { SubmitEvent } from "react";
import { ApiError } from "../api/client";
import type { SessionUser } from "../auth/auth";
import {
  ISSUE_DESCRIPTION_MAX,
  ISSUE_DESCRIPTION_MIN,
  ISSUE_TITLE_MAX,
  ISSUE_TITLE_MIN,
  createIssue,
  describeIssueError,
} from "../auth/issues";
import IssueLayout from "./IssueLayout";
import "../styles/IssueFormPage.css";

type ReportIssuePageProps = { user: SessionUser };
type FieldErrors = { title?: string; description?: string };

// Mirrors the backend rules so most mistakes are caught before a request;
// the server remains the authority and its field errors are shown as well.
function validate(title: string, description: string): FieldErrors {
  const errors: FieldErrors = {};
  const trimmedTitle = title.trim();
  const trimmedDescription = description.trim();
  if (trimmedTitle.length < ISSUE_TITLE_MIN || trimmedTitle.length > ISSUE_TITLE_MAX) {
    errors.title = `Use between ${ISSUE_TITLE_MIN} and ${ISSUE_TITLE_MAX} characters.`;
  }
  if (trimmedDescription.length < ISSUE_DESCRIPTION_MIN || trimmedDescription.length > ISSUE_DESCRIPTION_MAX) {
    errors.description = `Use between ${ISSUE_DESCRIPTION_MIN} and ${ISSUE_DESCRIPTION_MAX} characters.`;
  }
  return errors;
}

export default function ReportIssuePage({ user }: ReportIssuePageProps) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [error, setError] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSaving) return;
    const errors = validate(title, description);
    setFieldErrors(errors);
    setError("");
    if (errors.title || errors.description) return;

    setIsSaving(true);
    try {
      await createIssue({ title: title.trim(), description: description.trim() });
      window.location.href = "/issues";
    } catch (caught) {
      if (caught instanceof ApiError && caught.code === "VALIDATION_FAILED") {
        setFieldErrors({ title: caught.fields.title?.[0], description: caught.fields.description?.[0] });
      }
      setError(describeIssueError(caught));
      setIsSaving(false);
    }
  }

  return (
    <IssueLayout user={user} activePage="issues">
      <div className="issue-form-page">
        <a className="back-link" href="/issues">⮜ Back to issues</a>
        <h1>Report a new issue</h1>

        <form className="issue-form standalone-form" onSubmit={handleSubmit} noValidate>
          {error && <div className="issues-alert issues-alert-error" role="alert">{error}</div>}
          <label htmlFor="issue-title">Title <span className="required-mark">*</span></label>
          <input id="issue-title" value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Example: Water leak in bathroom" maxLength={ISSUE_TITLE_MAX} required aria-invalid={Boolean(fieldErrors.title)} aria-describedby="issue-title-hint" />
          <small className="issue-form-hint" id="issue-title-hint">{fieldErrors.title ?? `${ISSUE_TITLE_MIN}–${ISSUE_TITLE_MAX} characters.`}</small>

          <label htmlFor="issue-description">Description <span className="required-mark">*</span></label>
          <textarea id="issue-description" value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Describe the issue in detail." rows={7} maxLength={ISSUE_DESCRIPTION_MAX} required aria-invalid={Boolean(fieldErrors.description)} aria-describedby="issue-description-hint" />
          <small className="issue-form-hint" id="issue-description-hint">{fieldErrors.description ?? `${ISSUE_DESCRIPTION_MIN}–${ISSUE_DESCRIPTION_MAX} characters. Photo uploads are not available yet.`}</small>

          <div className="issue-form-actions">
            <a className="secondary-action button-link" href="/issues">Cancel</a>
            <button className="primary-action" type="submit" disabled={isSaving}>{isSaving ? "Submitting..." : "Submit issue"}</button>
          </div>
        </form>
      </div>
    </IssueLayout>
  );
}
