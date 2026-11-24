import {
  useEffect,
  useId,
  useRef,
  useState,
  type KeyboardEvent,
} from "react";
import "../styles/Residents.css";

type ConfirmResidentDeactivationProps = {
  open: boolean;
  residentName: string;
  onConfirm: () => Promise<void>;
  onClose: () => void;
};

function focusableElements(container: HTMLElement): HTMLElement[] {
  return Array.from(
    container.querySelectorAll<HTMLElement>(
      'button:not([disabled]), [href], [tabindex]:not([tabindex="-1"])',
    ),
  );
}

function ConfirmResidentDeactivationContent({
  residentName,
  onConfirm,
  onClose,
}: Omit<ConfirmResidentDeactivationProps, "open">) {
  const titleId = useId();
  const descriptionId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  const cancelRef = useRef<HTMLButtonElement>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    previousFocusRef.current =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
    cancelRef.current?.focus();

    return () => previousFocusRef.current?.focus();
  }, []);

  function closeDialog() {
    if (!isSubmitting) onClose();
  }

  function handleDialogKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === "Escape") {
      if (!isSubmitting) {
        event.preventDefault();
        closeDialog();
      }
      return;
    }

    if (event.key !== "Tab" || !dialogRef.current) return;
    const focusable = focusableElements(dialogRef.current);
    const first = focusable[0];
    const last = focusable.at(-1);
    if (!first || !last) {
      event.preventDefault();
      dialogRef.current.focus();
      return;
    }

    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }

  async function handleConfirm() {
    if (isSubmitting) return;

    setError("");
    dialogRef.current?.focus();
    setIsSubmitting(true);
    try {
      await onConfirm();
      onClose();
    } catch {
      setError(
        "Nu s-a putut dezactiva locatarul. Verificați conexiunea și încercați din nou.",
      );
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <div className="resident-dialog-backdrop">
      <div
        ref={dialogRef}
        className="resident-dialog resident-dialog--confirmation"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descriptionId}
        aria-busy={isSubmitting}
        tabIndex={-1}
        onKeyDown={handleDialogKeyDown}
      >
        <header className="resident-dialog-header">
          <div>
            <p className="dashboard-eyebrow">Confirmare</p>
            <h2 id={titleId}>Dezactivează locatarul</h2>
          </div>
        </header>

        <p id={descriptionId} className="resident-dialog-confirmation-copy">
          Dezactivarea contului <strong>{residentName}</strong> revocă sesiunile
          active și blochează autentificarea, fără a șterge istoricul sau
          înregistrările asociate.
        </p>

        {error && (
          <p className="resident-dialog-alert" role="alert">
            {error}
          </p>
        )}

        <footer className="resident-dialog-actions">
          <button
            ref={cancelRef}
            className="resident-dialog-button resident-dialog-button--secondary"
            type="button"
            disabled={isSubmitting}
            onClick={closeDialog}
          >
            Anulează
          </button>
          <button
            className="resident-dialog-button resident-dialog-button--danger"
            type="button"
            disabled={isSubmitting}
            onClick={handleConfirm}
          >
            {isSubmitting ? "Se dezactivează…" : "Dezactivează"}
          </button>
        </footer>
      </div>
    </div>
  );
}

export default function ConfirmResidentDeactivation(
  props: ConfirmResidentDeactivationProps,
) {
  if (!props.open) return null;

  return (
    <ConfirmResidentDeactivationContent
      residentName={props.residentName}
      onConfirm={props.onConfirm}
      onClose={props.onClose}
    />
  );
}
