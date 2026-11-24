import { useEffect, useRef, useState } from "react";
import type { DragEvent } from "react";
import { describeIssueError, listIssues, updateIssue } from "../../auth/issues";
import type { IssueChanges } from "../../auth/issues";
import { useAsyncResource } from "../../hooks/useAsyncResource";
import {
  issueLabel,
  moveIssue,
  priorityLabels,
  replaceIssue,
  reporterName,
  reprioritizeIssue,
  statuses,
  statusLabels,
} from "./issueModel";
import type { Issue, IssuePriority, IssueStatus } from "./issueModel";
import "./IssueTriage.css";

/**
 * One board change, captured as an object (command style): `apply` shows it
 * immediately, `changes` is what PATCH /api/issues/{id} receives, and the
 * issue's snapshot taken before `apply` undoes it if the server refuses.
 */
type IssueCommand = {
  issueId: string;
  changes: IssueChanges;
  apply: (issues: Issue[], now: string) => Issue[];
  successMessage: string;
};

function loadIssues(signal: AbortSignal) {
  return listIssues({ signal });
}

const dateFormat = new Intl.DateTimeFormat("en-GB", {
  day: "numeric",
  month: "short",
  year: "numeric",
  timeZone: "Europe/Chisinau",
});
const formatDate = (value: string) => dateFormat.format(new Date(value));
const priorityOptions: IssuePriority[] = [1, 2, 3, 4];
const dragType = "application/x-locatarius-issue";

type SelectOption = { value: string; label: string };

function TriageSelect({
  id,
  value,
  options,
  onChange,
  disabled = false,
}: {
  id: string;
  value: string;
  options: SelectOption[];
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  const [open, setOpen] = useState(false);
  const selected = options.find((option) => option.value === value) ?? options[0];

  return (
    <span className={`triage-select${disabled ? " triage-select-disabled" : ""}`}>
      <button
        id={id}
        className="triage-select-trigger"
        type="button"
        disabled={disabled}
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => setOpen((current) => !current)}
      >
        <span>{selected.label}</span>
        <span aria-hidden="true">⮟</span>
      </button>
      {open && !disabled && (
        <span className="triage-select-menu" role="listbox" aria-labelledby={id}>
          {options.map((option) => (
            <button
              className="triage-select-option"
              type="button"
              role="option"
              aria-selected={option.value === value}
              key={option.value}
              onClick={() => {
                onChange(option.value);
                setOpen(false);
              }}
            >
              {option.label}
            </button>
          ))}
        </span>
      )}
    </span>
  );
}

type IssueDetailsProps = {
  issue: Issue;
  saving: boolean;
  onClose: () => void;
  onStatusChange: (status: IssueStatus) => void;
  onPriorityChange: (priority: IssuePriority) => void;
};

function IssueDetails({
  issue,
  saving,
  onClose,
  onStatusChange,
  onPriorityChange,
}: IssueDetailsProps) {
  const dialog = useRef<HTMLDialogElement>(null);
  const closeButton = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    const element = dialog.current;
    element?.showModal();
    return () => {
      element?.close();
      // The card may have moved to a different column or sort position.
      const card = document.getElementById(`triage-card-${issue.issueId}`);
      if (card) card.focus();
      else document.getElementById("triage-priority")?.focus();
    };
  }, [issue.issueId]);

  useEffect(() => {
    if (issue.status === 3) closeButton.current?.focus();
  }, [issue.status]);

  function handleBackdropClick(event: React.MouseEvent<HTMLDialogElement>) {
    if (event.target === event.currentTarget) onClose();
  }

  return (
    <dialog
      ref={dialog}
      className="triage-dialog"
      aria-labelledby="issue-detail-title"
      onCancel={onClose}
      onClick={handleBackdropClick}
    >
      <div className="triage-dialog-header">
        <span className="triage-eyebrow">{issueLabel(issue)} · Issue details</span>
        <button
          type="button"
          className="triage-secondary"
          ref={closeButton}
          onClick={onClose}
          autoFocus
        >
          Close details
        </button>
      </div>

      <div className="triage-detail-body">
        <div className="triage-badges">
          <span className={`triage-status triage-status-${issue.status}`}>
            {statusLabels[issue.status]}
          </span>
          <span className={`triage-priority triage-priority-${issue.priority}`}>
            {priorityLabels[issue.priority]} priority
          </span>
        </div>
        <h2 id="issue-detail-title">{issue.title}</h2>
        <p className="triage-description">{issue.description}</p>
        <div className="triage-detail-overview">
          <dl className="triage-facts">
            <div><dt>Reported by</dt><dd>{reporterName(issue)}</dd></div>
            <div><dt>Apartment</dt><dd>{issue.reporter.apartment?.apartmentNumber ?? "Not assigned"}</dd></div>
            <div><dt>Reported on</dt><dd>{formatDate(issue.createdAt)}</dd></div>
            {issue.updatedAt && <div><dt>Last updated</dt><dd>{formatDate(issue.updatedAt)}</dd></div>}
            {issue.resolvedAt && <div><dt>Closed on</dt><dd>{formatDate(issue.resolvedAt)}</dd></div>}
          </dl>

          <section className="triage-detail-section" aria-labelledby="triage-action-heading">
            <label htmlFor="triage-detail-status">Status</label>
            <TriageSelect
              id="triage-detail-status"
              value={String(issue.status)}
              disabled={saving}
              onChange={(value) => onStatusChange(Number(value) as IssueStatus)}
              options={statuses.map((value) => ({ value: String(value), label: statusLabels[value] }))}
            />
            <label htmlFor="triage-detail-priority">Priority</label>
            <TriageSelect
              id="triage-detail-priority"
              value={String(issue.priority)}
              disabled={saving || issue.status === 3}
              onChange={(value) => onPriorityChange(Number(value) as IssuePriority)}
              options={priorityOptions.map((value) => ({ value: String(value), label: priorityLabels[value] }))}
            />
          </section>
        </div>

        <h3>Attached photo <span className="triage-muted">({issue.attachments.length})</span></h3>
        {issue.attachments.length ? issue.attachments.map((attachment) => (
          <figure className="triage-photo" key={attachment.attachmentId}>
            <a href={attachment.fileUrl} target="_blank" rel="noreferrer" aria-label="Open the full attached photo in a new tab">
              <img src={attachment.fileUrl} alt={`Photo attached to ${issue.title}`} />
            </a>
          </figure>
        )) : <p className="triage-no-photo">No photo was attached to this report.</p>}
      </div>
    </dialog>
  );
}

export default function IssueTriage() {
  const { resource, reload, update } = useAsyncResource(loadIssues);
  const issues = resource.status === "ready" ? resource.data : [];
  const [pendingIds, setPendingIds] = useState<ReadonlySet<string>>(() => new Set());
  const [saveError, setSaveError] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [priority, setPriority] = useState("all");
  const [sort, setSort] = useState("priority");
  const [announcement, setAnnouncement] = useState("");
  const [draggedId, setDraggedId] = useState<string | null>(null);
  const [dropStatus, setDropStatus] = useState<IssueStatus | null>(null);
  const selected = issues.find((issue) => issue.issueId === selectedId);
  const hasFilters = Boolean(query || priority !== "all");

  const visible = issues.filter((issue) => (
    `${issueLabel(issue)} ${issue.title} ${issue.description} ${reporterName(issue)} ${issue.reporter.apartment?.apartmentNumber ?? ""}`
      .toLowerCase().includes(query.trim().toLowerCase())
    && (priority === "all" || String(issue.priority) === priority)
  )).sort((a, b) => {
    if (sort === "newest") return b.createdAt.localeCompare(a.createdAt);
    if (sort === "oldest") return a.createdAt.localeCompare(b.createdAt);
    return b.priority - a.priority || b.createdAt.localeCompare(a.createdAt);
  });

  function setPending(id: string, pending: boolean) {
    setPendingIds((current) => {
      const next = new Set(current);
      if (pending) next.add(id);
      else next.delete(id);
      return next;
    });
  }

  // Invoker: optimistic apply → PATCH → keep the server copy, or roll back.
  // One request per issue at a time, so a rollback never overwrites a newer change.
  function run(command: IssueCommand) {
    const snapshot = issues.find((issue) => issue.issueId === command.issueId);
    if (!snapshot || pendingIds.has(command.issueId)) return;
    setSaveError("");
    update((current) => command.apply(current, new Date().toISOString()));
    setPending(command.issueId, true);
    updateIssue(command.issueId, command.changes)
      .then((saved) => {
        update((current) => replaceIssue(current, saved));
        setAnnouncement(command.successMessage);
      })
      .catch((error: unknown) => {
        update((current) => replaceIssue(current, snapshot));
        const message = `${issueLabel(snapshot)} was not changed: ${describeIssueError(error)}`;
        setSaveError(message);
        setAnnouncement(message);
      })
      .finally(() => setPending(command.issueId, false));
  }

  function changeStatus(id: string, status: IssueStatus) {
    const issue = issues.find((candidate) => candidate.issueId === id);
    if (!issue || issue.status === status) return;
    run({
      issueId: id,
      changes: { status },
      apply: (current, now) => moveIssue(current, id, status, now),
      successMessage: `${issueLabel(issue)} moved to ${statusLabels[status]}.`,
    });
  }

  function reprioritizeSelected(nextPriority: IssuePriority) {
    if (!selected || selected.status === 3 || selected.priority === nextPriority) return;
    const id = selected.issueId;
    run({
      issueId: id,
      changes: { priority: nextPriority },
      apply: (current, now) => reprioritizeIssue(current, id, nextPriority, now),
      successMessage: `${issueLabel(selected)} priority changed to ${priorityLabels[nextPriority]}.`,
    });
  }

  function startDrag(event: DragEvent<HTMLButtonElement>, id: string) {
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData(dragType, id);
    setDraggedId(id);
  }

  function dragOver(event: DragEvent<HTMLElement>, status: IssueStatus) {
    if (!draggedId) return;
    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
    if (dropStatus !== status) setDropStatus(status);
  }

  function dropIssue(event: DragEvent<HTMLElement>, status: IssueStatus) {
    event.preventDefault();
    const id = event.dataTransfer.getData(dragType);
    if (id && id === draggedId) changeStatus(id, status);
    setDraggedId(null);
    setDropStatus(null);
  }

  function clearFilters() {
    setQuery("");
    setPriority("all");
    setAnnouncement("");
  }

  return (
    <div className="issue-triage">
      <div className="triage-heading">
        <div>
          <h1>Issues</h1>
        </div>
      </div>

      <div className="triage-summary" aria-label="Issue overview">
        <div className="triage-summary-stat">
          <strong>{issues.filter((issue) => issue.status === 1).length}</strong>
          <span>Open</span>
        </div>

        <div className="triage-summary-stat">
          <strong>{issues.filter((issue) => issue.status === 2).length}</strong>
          <span>In progress</span>
        </div>

        <div className="triage-summary-stat">
          <strong>{issues.filter((issue) => issue.status === 3).length}</strong>
          <span>Closed</span>
        </div>
      </div>

      <div className="triage-toolbar">
        <div className="triage-search">
          <label htmlFor="triage-search">Search issues</label>
          <input id="triage-search" type="search" placeholder="Issue, resident or apartment…" value={query} onChange={(event) => setQuery(event.target.value)} />
        </div>
        <div className="triage-filter">
          <label htmlFor="triage-priority">Priority</label>
          <TriageSelect id="triage-priority" value={priority} onChange={setPriority} options={[{ value: "all", label: "All priorities" }, ...[4, 3, 2, 1].map((value) => ({ value: String(value), label: priorityLabels[value as IssuePriority] }))]} />
        </div>
        <div className="triage-filter">
          <label htmlFor="triage-sort">Sort cards</label>
          <TriageSelect id="triage-sort" value={sort} onChange={setSort} options={[{ value: "priority", label: "Highest priority" }, { value: "newest", label: "Newest first" }, { value: "oldest", label: "Oldest first" }]} />
        </div>
      </div>
      <div className="triage-toolbar-meta">
        <p className="triage-results" role="status">
          {resource.status === "loading" ? "Loading issues…" : announcement || `Showing ${visible.length} of ${issues.length} issues`}
        </p>
        {hasFilters && <button type="button" className="triage-clear" onClick={clearFilters}>Clear filters</button>}
      </div>
      {saveError && <div className="issues-alert issues-alert-error" role="alert">{saveError}</div>}
      {resource.status === "error" && (
        <div className="issues-alert issues-alert-error" role="alert">
          {describeIssueError(resource.error)}{" "}
          <button type="button" className="triage-secondary" onClick={reload}>Try again</button>
        </div>
      )}
      {resource.status === "ready" && !visible.length && (
        <div className="triage-empty-results">
          <strong>No matching issues</strong>
          <p>Try another search or clear the filters to see all reports.</p>
        </div>
      )}
      <div className="triage-board" aria-busy={resource.status === "loading"}>
        {statuses.map((status) => {
          const column = visible.filter((issue) => issue.status === status);
          return (
            <section
              className={`triage-column triage-column-${status}${dropStatus === status ? " triage-drop-target" : ""}`}
              key={status}
              aria-labelledby={`triage-column-${status}`}
              onDragOver={(event) => dragOver(event, status)}
              onDrop={(event) => dropIssue(event, status)}
            >
              <header className="triage-column-heading">
                <h2 id={`triage-column-${status}`}><span className={`triage-dot triage-dot-${status}`} />{statusLabels[status]}</h2>
                <span className="triage-count">{column.length}</span>
              </header>
              <p className="triage-column-caption">{status === 1 ? "" : status === 2 ? "" : ""}</p>
              <div className="triage-cards">
                {column.map((issue) => (
                  <button
                    className={`triage-card${draggedId === issue.issueId ? " triage-card-dragging" : ""}`}
                    id={`triage-card-${issue.issueId}`}
                    type="button"
                    key={issue.issueId}
                    draggable={!pendingIds.has(issue.issueId)}
                    aria-busy={pendingIds.has(issue.issueId)}
                    onDragStart={(event) => startDrag(event, issue.issueId)}
                    onDragEnd={() => { setDraggedId(null); setDropStatus(null); }}
                    onClick={() => setSelectedId(issue.issueId)}
                    aria-label={`View ${issueLabel(issue)}: ${issue.title}`}
                    aria-describedby="triage-board-help"
                  >
                    <span className="triage-card-top">
                      <span>{issueLabel(issue)}</span>
                      <span className={`triage-priority triage-priority-${issue.priority}`}>{priorityLabels[issue.priority]}</span>
                    </span>
                    <strong className="triage-card-title">{issue.title}</strong>
                    <span className="triage-card-location">Reported by {reporterName(issue)}</span>
                    {issue.attachments.length > 0 && (
                      <span className="triage-card-tags"><span>↗ {issue.attachments.length} photo</span></span>
                    )}
                    <span className="triage-card-footer">
                      <span>{statusLabels[issue.status]}<small>Apartment {issue.reporter.apartment?.apartmentNumber ?? "—"}</small></span>
                      <time dateTime={issue.createdAt}>{formatDate(issue.createdAt)}</time>
                    </span>
                  </button>
                ))}
                {resource.status === "ready" && !column.length && <p className="triage-column-empty">{status === 3 ? "Completed issues will appear here." : "No issues in this stage."}</p>}
              </div>
            </section>
          );
        })}
      </div>
      {selected && (
        <IssueDetails
          key={selected.issueId}
          issue={selected}
          saving={pendingIds.has(selected.issueId)}
          onClose={() => { setSelectedId(null); setAnnouncement(""); }}
          onStatusChange={(status) => changeStatus(selected.issueId, status)}
          onPriorityChange={reprioritizeSelected}
        />
      )}
    </div>
  );
}
