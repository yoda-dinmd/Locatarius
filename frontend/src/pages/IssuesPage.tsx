import { useMemo, useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import type { SessionUser } from "../auth/auth";
import {
  createIssue,
  getAllIssues,
  getFilterOptions,
  getIssuesForResident,
  type IssueFilter,
} from "../auth/issues";
import "../styles/IssuesPage.css";

type IssuesPageProps = {
  user: SessionUser;
};

const EMPTY_FORM = {
  title: "",
  description: "",
};

export default function IssuesPage({ user }: IssuesPageProps) {
  const [statusFilter, setStatusFilter] = useState<IssueFilter>("All");
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [formState, setFormState] = useState(EMPTY_FORM);
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  const issues =
    user.role === "admin" ? getAllIssues() : getIssuesForResident(user.id);

  const visibleIssues = useMemo(() => {
    if (statusFilter === "All") {
      return issues;
    }

    return issues.filter((issue) => issue.status === statusFilter);
  }, [issues, statusFilter]);

  function handleFormChange(
    event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
  ) {
    const { name, value } = event.target;

    setFormState((current) => ({
      ...current,
      [name]: value,
    }));
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (user.role !== "resident") {
      return;
    }

    const trimmedTitle = formState.title.trim();
    const trimmedDescription = formState.description.trim();

    if (!trimmedTitle || !trimmedDescription) {
      setError("Title and description are required.");
      return;
    }

    const fileInput = event.currentTarget.elements.namedItem(
      "evidence",
    ) as HTMLInputElement | null;
    const selectedFile = fileInput?.files?.[0];

    const createdIssue = createIssue({
      residentId: user.id,
      buildingId: "BLD-01",
      title: trimmedTitle,
      description: trimmedDescription,
      evidenceName: selectedFile?.name,
      evidenceType: selectedFile?.type,
    });

    setSuccessMessage(`Issue "${createdIssue.title}" was submitted and marked as Open.`);
    setError("");
    setFormState(EMPTY_FORM);
    setIsFormOpen(false);
  }

  return (
    <main className="issues-page">
      <header className="issues-header">
        <div className="issues-brand-block">
          <a className="issues-brand" href="/dashboard">
            <span className="brand-mark">L</span>
            <span>Locatarius</span>
          </a>
        </div>

        <div className="issues-header-actions">
          <a className="issues-header-link" href="/dashboard">
            Dashboard
          </a>
          <a className="issues-header-link active" href="/issues">
            Issues
          </a>
        </div>
      </header>

      <section className="issues-content">
        <div className="issues-toolbar">
          <div>
            <p className="issues-kicker">Issue reports</p>
            <h1>{user.role === "admin" ? "Building issues" : "My issues"}</h1>
          </div>

          {user.role === "resident" && (
            <button
              className="primary-action"
              type="button"
              onClick={() => setIsFormOpen((current) => !current)}
            >
              {isFormOpen ? "Close form" : "Report Issue"}
            </button>
          )}
        </div>

        {user.role === "admin" && (
          <div className="issues-admin-banner">
            Admin issue management is not yet implemented in this mock view.
          </div>
        )}

        {error && (
          <div className="issues-alert issues-alert-error" role="alert">
            {error}
          </div>
        )}

        {successMessage && (
          <div className="issues-alert issues-alert-success" role="status">
            {successMessage}
          </div>
        )}

        {user.role === "resident" && isFormOpen && (
          <form className="issue-form" onSubmit={handleSubmit}>
            <div className="issue-form-header">
              <h2>Report a new issue</h2>
            </div>

            <label htmlFor="issue-title">Title</label>
            <input
              id="issue-title"
              name="title"
              type="text"
              value={formState.title}
              onChange={handleFormChange}
              placeholder="Example: Water leak in bathroom"
            />

            <label htmlFor="issue-description">Description</label>
            <textarea
              id="issue-description"
              name="description"
              value={formState.description}
              onChange={handleFormChange}
              placeholder="Describe the issue in detail."
              rows={5}
            />

            <label htmlFor="issue-evidence">Evidence (optional)</label>
            <input
              id="issue-evidence"
              name="evidence"
              type="file"
              accept="image/*,video/*"
            />
            <small className="issue-form-hint">
              Upload a photo or short video. Recommended file size: up to 10 MB.
            </small>

            <div className="issue-form-actions">
              <button
                className="secondary-action"
                type="button"
                onClick={() => setIsFormOpen(false)}
              >
                Cancel
              </button>
              <button className="primary-action" type="submit">
                Submit issue
              </button>
            </div>
          </form>
        )}

        <div className="issues-filters" aria-label="Filter issues by status">
          {getFilterOptions().map((option) => (
            <button
              key={option}
              type="button"
              className={statusFilter === option ? "filter-button active" : "filter-button"}
              onClick={() => setStatusFilter(option)}
            >
              {option}
            </button>
          ))}
        </div>

        <div className="issue-list">
          {visibleIssues.length === 0 ? (
            <div className="issue-empty-state">
              No issues match the selected status.
            </div>
          ) : (
            visibleIssues.map((issue) => (
              <article className="issue-card" key={issue.id}>
                <div className="issue-card-header">
                  <div>
                    <p className="issue-meta">
                      {issue.buildingId} · {new Date(issue.createdAt).toLocaleDateString()}
                    </p>
                    <h3>{issue.title}</h3>
                  </div>

                  <span className={`status-badge status-${issue.status.toLowerCase().replace(/\s+/g, "-")}`}>
                    {issue.status}
                  </span>
                </div>

                <p className="issue-description">{issue.description}</p>

                {issue.evidenceName ? (
                  <div className="issue-evidence">
                    Evidence: {issue.evidenceName}
                  </div>
                ) : (
                  <div className="issue-evidence muted">No evidence attached.</div>
                )}
              </article>
            ))
          )}
        </div>
      </section>
    </main>
  );
}
