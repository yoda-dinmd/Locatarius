import { useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import type { SessionUser } from "../auth/auth";
import { closeIssue, getAllIssues, type IssueEvidence } from "../auth/issues";
import IssueLayout from "./IssueLayout";
import "../styles/IssueFormPage.css";

type CloseIssuePageProps = { user: SessionUser; issueId: string };
type SelectedEvidence = IssueEvidence & { file: File };
const MAX_EVIDENCE_FILES = 5;
const MAX_FILE_SIZE = 10 * 1024 * 1024;

function readFile(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(new Error("Unable to read file."));
    reader.readAsDataURL(file);
  });
}

export default function CloseIssuePage({ user, issueId }: CloseIssuePageProps) {
  const issue = getAllIssues().find((candidate) => candidate.id === issueId);
  const [description, setDescription] = useState("");
  const [evidence, setEvidence] = useState<SelectedEvidence[]>([]);
  const [error, setError] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  if (!issue || user.role !== "admin") {
    return <IssueLayout user={user} activePage="issues"><div className="issue-form-page"><h1>Issue not found</h1><a className="back-link" href="/issues">Back to issues</a></div></IssueLayout>;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!description.trim() || evidence.length === 0) {
      setError("Description and evidence of fix are required.");
      return;
    }
    setIsSaving(true);
    try {
      const evidenceFiles = await Promise.all(evidence.map(async (item) => ({
        name: item.name,
        type: item.type,
        url: await readFile(item.file),
      })));
      const firstEvidence = evidenceFiles[0];
      closeIssue({ issueId, description, evidenceName: firstEvidence.name, evidenceType: firstEvidence.type, evidenceUrl: firstEvidence.url, fixEvidenceFiles: evidenceFiles, closedBy: user.name });
      window.location.href = `/issues/${issueId}`;
    } catch {
      setError("The evidence file could not be uploaded.");
      setIsSaving(false);
    }
  }

  function handleEvidenceChange(event: ChangeEvent<HTMLInputElement>) {
    const selectedFiles = Array.from(event.target.files ?? []);
    const remainingSlots = MAX_EVIDENCE_FILES - evidence.length;
    const validFiles = selectedFiles.filter((file) => file.type.startsWith("image/") || file.type.startsWith("video/"));

    if (selectedFiles.length > remainingSlots || validFiles.some((file) => file.size > MAX_FILE_SIZE)) {
      setError("You can upload up to 5 image or video files, with a maximum of 10 MB per file.");
      event.currentTarget.value = "";
      return;
    }

    setEvidence((current) => [...current, ...validFiles.map((file) => ({
      file,
      name: file.name,
      type: file.type,
      url: URL.createObjectURL(file),
    }))]);
    setError("");
    event.currentTarget.value = "";
  }

  function removeEvidence(index: number) {
    setEvidence((current) => {
      URL.revokeObjectURL(current[index].url);
      return current.filter((_, itemIndex) => itemIndex !== index);
    });
  }

  return (
    <IssueLayout user={user} activePage="issues">
      <div className="issue-form-page">
        <a className="back-link" href={`/issues/${issueId}`}>⮜ Return to issue</a>
        <p className="issues-kicker">Close issue</p>
        <h1>{issue.title}</h1>
        <form className="issue-form standalone-form" onSubmit={handleSubmit}>
          {error && <div className="issues-alert issues-alert-error" role="alert">{error}</div>}
          <label htmlFor="fix-description">Description of fix <span className="required-mark">*</span></label>
          <textarea id="fix-description" value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Describe how the issue was resolved." rows={7} required />
          <label htmlFor="fix-evidence">Evidence of fix <span className="required-mark">*</span></label>
          <div className="file-picker">
            <label className="file-picker-button" htmlFor="fix-evidence">Choose files</label>
            <input className="file-picker-input" id="fix-evidence" type="file" accept="image/*,video/*" multiple onChange={handleEvidenceChange} required={evidence.length === 0} />
            {evidence.length > 0 && <div className="file-picker-previews">
              {evidence.map((item, index) => <span className="file-picker-preview" title={item.name} key={`${item.name}-${index}`}>
                {item.type.startsWith("video/") ? <video src={item.url} muted /> : <img src={item.url} alt={`Selected evidence ${index + 1}`} />}
                <button type="button" className="file-picker-remove" onClick={() => removeEvidence(index)} aria-label={`Remove ${item.name}`}>×</button>
              </span>)}
            </div>}
          </div>
          <small className="issue-form-hint">Upload a photo or short video showing the completed fix.</small>
          <div className="issue-form-actions"><a className="secondary-action button-link" href={`/issues/${issueId}`}>Cancel</a><button className="primary-action" type="submit" disabled={isSaving}>{isSaving ? "Closing..." : "Close issue"}</button></div>
        </form>
      </div>
    </IssueLayout>
  );
}
