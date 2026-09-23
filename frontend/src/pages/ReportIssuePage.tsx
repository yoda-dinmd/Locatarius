import { useState } from "react";
import type { ChangeEvent, SubmitEvent } from "react";
import type { SessionUser } from "../auth/auth";
import { createIssue, getBuildingForResident, type IssueEvidence } from "../auth/issues";
import IssueLayout from "./IssueLayout";
import "../styles/IssueFormPage.css";

type ReportIssuePageProps = { user: SessionUser };
type SelectedEvidence = IssueEvidence & { file: File };
const MAX_EVIDENCE_FILES = 5;
const MAX_FILE_SIZE = 10 * 1024 * 1024;

function readFile(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(new Error("The selected file could not be read."));
    reader.readAsDataURL(file);
  });
}

export default function ReportIssuePage({ user }: ReportIssuePageProps) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [evidence, setEvidence] = useState<SelectedEvidence[]>([]);
  const [error, setError] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!title.trim() || !description.trim() || evidence.length === 0) {
      setError("Title, description, and evidence are required.");
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
      createIssue({
        residentId: user.id,
        reporterName: user.name,
        buildingId: getBuildingForResident(user.id),
        title,
        description,
        evidenceName: firstEvidence.name,
        evidenceType: firstEvidence.type,
        evidenceUrl: firstEvidence.url,
        evidenceFiles,
      });
      window.location.href = "/issues";
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
        <a className="back-link" href="/issues">⮜ Back to issues</a>
        <p className="issues-kicker">Issue reports</p>
        <h1>Report a new issue</h1>

        <form className="issue-form standalone-form" onSubmit={handleSubmit}>
          {error && <div className="issues-alert issues-alert-error" role="alert">{error}</div>}
          <label htmlFor="issue-title">Title <span className="required-mark">*</span></label>
          <input id="issue-title" value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Example: Water leak in bathroom" required />

          <label htmlFor="issue-description">Description <span className="required-mark">*</span></label>
          <textarea id="issue-description" value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Describe the issue in detail." rows={7} required />

          <label htmlFor="issue-evidence">Evidence <span className="required-mark">*</span></label>
          <div className="file-picker">
            <label className="file-picker-button" htmlFor="issue-evidence">Choose files</label>
            <input className="file-picker-input" id="issue-evidence" type="file" accept="image/*,video/*" multiple onChange={handleEvidenceChange} required={evidence.length === 0} />
            {evidence.length > 0 && <div className="file-picker-previews">
              {evidence.map((item, index) => <span className="file-picker-preview" title={item.name} key={`${item.name}-${index}`}>
                {item.type.startsWith("video/") ? <video src={item.url} muted /> : <img src={item.url} alt={`Selected evidence ${index + 1}`} />}
                <button type="button" className="file-picker-remove" onClick={() => removeEvidence(index)} aria-label={`Remove ${item.name}`}>×</button>
              </span>)}
            </div>}
          </div>
          <small className="issue-form-hint">Upload a photo or short video. Recommended file size: up to 10 MB.</small>

          <div className="issue-form-actions">
            <a className="secondary-action button-link" href="/issues">Cancel</a>
            <button className="primary-action" type="submit" disabled={isSaving}>{isSaving ? "Submitting..." : "Submit issue"}</button>
          </div>
        </form>
      </div>
    </IssueLayout>
  );
}
