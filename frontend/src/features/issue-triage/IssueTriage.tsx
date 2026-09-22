import { useEffect, useRef, useState } from "react";
import issuesMock from "./issuesMock.json";
import {
  advanceIssue,
  priorityLabels,
  reprioritizeIssue,
  statuses,
  statusLabels,
} from "./issueModel";
import type { Issue, IssuePriority } from "./issueModel";
import meterPhoto from "./assets/broken-water-meter.jpg";
import "./IssueTriage.css";

const dateFormat = new Intl.DateTimeFormat("en-GB", {
  day: "numeric",
  month: "short",
  year: "numeric",
  timeZone: "Europe/Chisinau",
});
const formatDate = (value: string) => dateFormat.format(new Date(value));
const priorityOptions: IssuePriority[] = [1, 2, 3, 4];

type IssueDetailsProps = {
  issue: Issue;
  onClose: () => void;
  onAdvance: () => void;
  onPriorityChange: (priority: IssuePriority) => void;
};

function IssueDetails({
  issue,
  onClose,
  onAdvance,
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
        <span className="triage-eyebrow">{issue.reference} · Issue details</span>
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
        <dl className="triage-facts">
          <div><dt>Reported by</dt><dd>{issue.reporterName} · Apt. {issue.apartment}</dd></div>
          <div><dt>Location</dt><dd>{issue.location}</dd></div>
          <div><dt>Reported on</dt><dd>{formatDate(issue.createdAt)}</dd></div>
          {issue.updatedAt && <div><dt>Last updated</dt><dd>{formatDate(issue.updatedAt)}</dd></div>}
          {issue.resolvedAt && <div><dt>Closed on</dt><dd>{formatDate(issue.resolvedAt)}</dd></div>}
        </dl>

        <section className="triage-detail-section" aria-labelledby="triage-action-heading">
          <h3 id="triage-action-heading">Triage</h3>
          <p className="triage-section-copy">Set the priority of an active issue.</p>
          <label htmlFor="triage-detail-priority">Priority</label>
          <select
            id="triage-detail-priority"
            value={issue.priority}
            disabled={issue.status === 3}
            onChange={(event) => onPriorityChange(Number(event.target.value) as IssuePriority)}
          >
            {priorityOptions.map((value) => (
              <option value={value} key={value}>{priorityLabels[value]}</option>
            ))}
          </select>
        </section>

        <h3>Attached photo <span className="triage-muted">({issue.attachments.length})</span></h3>
        {issue.attachments.length ? issue.attachments.map((attachment) => (
          <figure className="triage-photo" key={attachment.attachmentId}>
            <a href={meterPhoto} target="_blank" rel="noreferrer" aria-label="Open full water meter photo in a new tab">
              <img src={meterPhoto} alt={attachment.alt} />
            </a>
            <figcaption>
              Sample photo · <a href="https://commons.wikimedia.org/wiki/File:Broken_water_meter_shows_measurement_in_residential_area.jpg" target="_blank" rel="noreferrer">Shixart1985 / Wikimedia Commons</a> · <a href="https://creativecommons.org/licenses/by/2.0/" target="_blank" rel="noreferrer">CC BY 2.0</a>
            </figcaption>
          </figure>
        )) : <p className="triage-no-photo">No photo was attached to this report.</p>}
      </div>

      <footer className="triage-detail-footer">
        <div aria-live="polite">
          <strong>{statusLabels[issue.status]}</strong>
          <p>{issue.status === 3 ? "This issue is closed. No further action is needed." : "Move this issue to the next stage when ready."}</p>
        </div>
        {issue.status !== 3 && (
          <button className="triage-primary" type="button" onClick={onAdvance}>
            {issue.status === 1 ? "Start work" : "Close issue"}<span aria-hidden="true"> →</span>
          </button>
        )}
      </footer>
    </dialog>
  );
}

export default function IssueTriage() {
  const [issues, setIssues] = useState<Issue[]>(() => structuredClone(issuesMock) as Issue[]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [priority, setPriority] = useState("all");
  const [sort, setSort] = useState("priority");
  const [announcement, setAnnouncement] = useState("");
  const selected = issues.find((issue) => issue.issueId === selectedId);
  const hasFilters = Boolean(query || priority !== "all");

  const visible = issues.filter((issue) => (
    `${issue.reference} ${issue.title} ${issue.description} ${issue.reporterName} ${issue.apartment}`
      .toLowerCase().includes(query.trim().toLowerCase())
    && (priority === "all" || String(issue.priority) === priority)
  )).sort((a, b) => {
    if (sort === "newest") return b.createdAt.localeCompare(a.createdAt);
    if (sort === "oldest") return a.createdAt.localeCompare(b.createdAt);
    return b.priority - a.priority || b.createdAt.localeCompare(a.createdAt);
  });

  function advanceSelected() {
    if (!selected || selected.status === 3) return;
    setIssues((current) => advanceIssue(current, selected.issueId, selected.status, new Date().toISOString()));
    setAnnouncement(`${selected.reference} moved to ${statusLabels[selected.status === 1 ? 2 : 3]}.`);
  }

  function reprioritizeSelected(nextPriority: IssuePriority) {
    if (!selected || selected.status === 3) return;
    setIssues((current) => reprioritizeIssue(current, selected.issueId, nextPriority, new Date().toISOString()));
    setAnnouncement(`${selected.reference} priority changed to ${priorityLabels[nextPriority]}.`);
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
          <p className="triage-eyebrow">Building management · Teilor 12</p>
          <h1>Issues</h1>
          <p className="triage-intro">Review reports from your building and keep repairs moving.</p>
        </div>
        <span className="triage-demo">Presentation demo</span>
      </div>

      <div className="triage-summary" aria-label="Issue overview">
        <div className="triage-summary-stat"><strong>{issues.filter((issue) => issue.status === 1).length}</strong><span>Open</span></div>
        <div className="triage-summary-stat"><strong>{issues.filter((issue) => issue.status === 2).length}</strong><span>In progress</span></div>
        <div className="triage-summary-stat"><strong>{issues.filter((issue) => issue.status === 3).length}</strong><span>Closed</span></div>
        <p className="triage-demo-note">{issues.length} sample reports · changes reset on refresh</p>
      </div>

      <div className="triage-toolbar">
        <div className="triage-search">
          <label htmlFor="triage-search">Search issues</label>
          <input id="triage-search" type="search" placeholder="Issue, resident or apartment…" value={query} onChange={(event) => setQuery(event.target.value)} />
        </div>
        <div className="triage-filter">
          <label htmlFor="triage-priority">Priority</label>
          <select id="triage-priority" value={priority} onChange={(event) => setPriority(event.target.value)}>
            <option value="all">All priorities</option>
            {[4, 3, 2, 1].map((value) => <option key={value} value={value}>{priorityLabels[value as IssuePriority]}</option>)}
          </select>
        </div>
        <div className="triage-filter">
          <label htmlFor="triage-sort">Sort cards</label>
          <select id="triage-sort" value={sort} onChange={(event) => setSort(event.target.value)}>
            <option value="priority">Highest priority</option>
            <option value="newest">Newest first</option>
            <option value="oldest">Oldest first</option>
          </select>
        </div>
      </div>
      <div className="triage-toolbar-meta">
        <p className="triage-results" role="status">{announcement || `Showing ${visible.length} of ${issues.length} issues`}</p>
        {hasFilters && <button type="button" className="triage-clear" onClick={clearFilters}>Clear filters</button>}
      </div>
      {!visible.length && (
        <div className="triage-empty-results">
          <strong>No matching issues</strong>
          <p>Try another search or clear the filters to see all reports.</p>
        </div>
      )}
      <div className="triage-board">
        {statuses.map((status) => {
          const column = visible.filter((issue) => issue.status === status);
          return (
            <section className={`triage-column triage-column-${status}`} key={status} aria-labelledby={`triage-column-${status}`}>
              <header className="triage-column-heading">
                <h2 id={`triage-column-${status}`}><span className={`triage-dot triage-dot-${status}`} />{statusLabels[status]}</h2>
                <span className="triage-count">{column.length}</span>
              </header>
              <p className="triage-column-caption">{status === 1 ? "Ready for review" : status === 2 ? "Repairs under way" : "Resolved and completed"}</p>
              <div className="triage-cards">
                {column.map((issue) => (
                  <button
                    className="triage-card"
                    id={`triage-card-${issue.issueId}`}
                    type="button"
                    key={issue.issueId}
                    onClick={() => setSelectedId(issue.issueId)}
                    aria-label={`View ${issue.reference}: ${issue.title}`}
                  >
                    <span className="triage-card-top">
                      <span>{issue.reference}</span>
                      <span className={`triage-priority triage-priority-${issue.priority}`}>{priorityLabels[issue.priority]}</span>
                    </span>
                    <strong className="triage-card-title">{issue.title}</strong>
                    <span className="triage-card-location">{issue.location}</span>
                    {issue.attachments.length > 0 && (
                      <span className="triage-card-tags"><span>↗ {issue.attachments.length} photo</span></span>
                    )}
                    <span className="triage-card-footer">
                      <span>{issue.reporterName}<small>Apartment {issue.apartment}</small></span>
                      <time dateTime={issue.createdAt}>{formatDate(issue.createdAt)}</time>
                    </span>
                  </button>
                ))}
                {!column.length && <p className="triage-column-empty">{status === 3 ? "Completed issues will appear here." : "No issues in this stage."}</p>}
              </div>
            </section>
          );
        })}
      </div>
      <p className="triage-board-help">Select a report to review details, set priority and update progress.</p>
      {selected && (
        <IssueDetails
          key={selected.issueId}
          issue={selected}
          onClose={() => { setSelectedId(null); setAnnouncement(""); }}
          onAdvance={advanceSelected}
          onPriorityChange={reprioritizeSelected}
        />
      )}
    </div>
  );
}
