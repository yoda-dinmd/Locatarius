import {
  useEffect,
  useId,
  useRef,
  useState,
  type FormEvent,
  type KeyboardEvent,
} from "react";
import { ApiError } from "../api/client";
import "../styles/Residents.css";

export type ApartmentOptionDto = {
  id: string;
  apartment: string;
  building: string;
};

export type ResidentFormValues = {
  firstName: string;
  lastName: string;
  email: string;
  apartmentId: string;
  isActive: boolean;
};

export type CreateResidentInput = Omit<ResidentFormValues, "isActive"> & {
  temporaryPassword: string;
  confirmPassword: string;
};

export type UpdateResidentInput = ResidentFormValues;

type ResidentField =
  | "firstName"
  | "lastName"
  | "email"
  | "apartmentId"
  | "temporaryPassword"
  | "confirmPassword";

type FieldErrors = Partial<Record<ResidentField, string[]>>;

type ResidentFormDialogProps = {
  open: boolean;
  mode: "create" | "edit";
  apartments: readonly ApartmentOptionDto[];
  resident?: ResidentFormValues;
  onSubmit: (values: CreateResidentInput | UpdateResidentInput) => Promise<void>;
  onClose: () => void;
};

const EMPTY_VALUES: ResidentFormValues = {
  firstName: "",
  lastName: "",
  email: "",
  apartmentId: "",
  isActive: true,
};

const EMAIL_EXISTS_MESSAGE =
  "Există deja un cont cu această adresă de email.";
const NETWORK_MESSAGE =
  "Nu s-a putut salva locatarul. Verificați conexiunea și încercați din nou.";

function pickFieldErrors(fields: Record<string, string[]>): FieldErrors {
  const knownFields: ResidentField[] = [
    "firstName",
    "lastName",
    "email",
    "apartmentId",
    "temporaryPassword",
    "confirmPassword",
  ];
  const result: FieldErrors = {};

  for (const field of knownFields) {
    if (fields[field]?.length) result[field] = fields[field];
  }

  return result;
}

function focusableElements(container: HTMLElement): HTMLElement[] {
  return Array.from(
    container.querySelectorAll<HTMLElement>(
      'button:not([disabled]), input:not([disabled]), select:not([disabled]), [href], [tabindex]:not([tabindex="-1"])',
    ),
  );
}

function ResidentFormDialogContent({
  mode,
  apartments,
  resident,
  onSubmit,
  onClose,
}: Omit<ResidentFormDialogProps, "open">) {
  const titleId = useId();
  const descriptionId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  const firstNameRef = useRef<HTMLInputElement>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);
  const [values, setValues] = useState<ResidentFormValues>(() =>
    mode === "edit" && resident ? { ...resident } : { ...EMPTY_VALUES },
  );
  const [temporaryPassword, setTemporaryPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [formError, setFormError] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  function clearPasswords() {
    setTemporaryPassword("");
    setConfirmPassword("");
  }

  useEffect(() => {
    previousFocusRef.current =
      document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
    firstNameRef.current?.focus();

    return () => previousFocusRef.current?.focus();
  }, []);

  function closeDialog() {
    if (isSubmitting) return;
    clearPasswords();
    setFieldErrors({});
    setFormError("");
    onClose();
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

  function fieldDescription(field: ResidentField) {
    return fieldErrors[field]?.length ? `${field}-error` : undefined;
  }

  function renderFieldError(field: ResidentField) {
    const messages = fieldErrors[field];
    if (!messages?.length) return null;

    return (
      <p id={`${field}-error`} className="resident-dialog-field-error">
        {messages.join(" ")}
      </p>
    );
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    setFieldErrors({});
    setFormError("");

    if (mode === "create") {
      const credentialErrors: FieldErrors = {};
      if (temporaryPassword.length < 15 || temporaryPassword.length > 128) {
        credentialErrors.temporaryPassword = [
          "Parola temporară trebuie să aibă între 15 și 128 de caractere.",
        ];
      }
      if (confirmPassword !== temporaryPassword) {
        credentialErrors.confirmPassword = [
          "Confirmarea parolei temporare nu coincide.",
        ];
      }
      if (Object.keys(credentialErrors).length > 0) {
        setFieldErrors(credentialErrors);
        return;
      }
    }

    dialogRef.current?.focus();
    setIsSubmitting(true);

    const submission: CreateResidentInput | UpdateResidentInput =
      mode === "create"
        ? {
            firstName: values.firstName,
            lastName: values.lastName,
            email: values.email,
            apartmentId: values.apartmentId,
            temporaryPassword,
            confirmPassword,
          }
        : { ...values };

    try {
      await onSubmit(submission);
      clearPasswords();
      onClose();
    } catch (caught) {
      clearPasswords();

      if (caught instanceof ApiError) {
        setFieldErrors(pickFieldErrors(caught.fields));
        if (caught.status === 409 && caught.code === "EMAIL_EXISTS") {
          setFormError(EMAIL_EXISTS_MESSAGE);
        } else if (Object.keys(caught.fields).length === 0) {
          setFormError(caught.message || NETWORK_MESSAGE);
        }
      } else {
        setFormError(NETWORK_MESSAGE);
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  const title = mode === "create" ? "Adaugă locatar" : "Editează locatarul";

  return (
    <div className="resident-dialog-backdrop">
      <div
        ref={dialogRef}
        className="resident-dialog"
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
            <p className="dashboard-eyebrow">Administrare locatari</p>
            <h2 id={titleId}>{title}</h2>
            <p id={descriptionId}>
              {mode === "create"
                ? "Creați un cont Resident și asociați-l unui apartament."
                : "Actualizați datele și starea contului Resident."}
            </p>
          </div>
          <button
            className="resident-dialog-close"
            type="button"
            aria-label="Închide"
            disabled={isSubmitting}
            onClick={closeDialog}
          >
            ×
          </button>
        </header>

        {formError && (
          <p className="resident-dialog-alert" role="alert">
            {formError}
          </p>
        )}

        <form className="resident-dialog-form" onSubmit={handleSubmit}>
          <div className="resident-dialog-grid">
            <div className="resident-dialog-field">
              <label htmlFor="firstName">Prenume</label>
              <input
                ref={firstNameRef}
                id="firstName"
                name="firstName"
                value={values.firstName}
                maxLength={100}
                required
                disabled={isSubmitting}
                aria-invalid={fieldErrors.firstName ? true : undefined}
                aria-describedby={fieldDescription("firstName")}
                onChange={(event) =>
                  setValues((current) => ({
                    ...current,
                    firstName: event.target.value,
                  }))
                }
              />
              {renderFieldError("firstName")}
            </div>

            <div className="resident-dialog-field">
              <label htmlFor="lastName">Nume</label>
              <input
                id="lastName"
                name="lastName"
                value={values.lastName}
                maxLength={100}
                required
                disabled={isSubmitting}
                aria-invalid={fieldErrors.lastName ? true : undefined}
                aria-describedby={fieldDescription("lastName")}
                onChange={(event) =>
                  setValues((current) => ({
                    ...current,
                    lastName: event.target.value,
                  }))
                }
              />
              {renderFieldError("lastName")}
            </div>
          </div>

          <div className="resident-dialog-field">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="email"
              value={values.email}
              maxLength={254}
              required
              disabled={isSubmitting}
              aria-invalid={fieldErrors.email ? true : undefined}
              aria-describedby={fieldDescription("email")}
              onChange={(event) =>
                setValues((current) => ({
                  ...current,
                  email: event.target.value,
                }))
              }
            />
            {renderFieldError("email")}
          </div>

          <div className="resident-dialog-field">
            <label htmlFor="apartmentId">Apartament</label>
            <select
              id="apartmentId"
              name="apartmentId"
              value={values.apartmentId}
              required
              disabled={isSubmitting}
              aria-invalid={fieldErrors.apartmentId ? true : undefined}
              aria-describedby={fieldDescription("apartmentId")}
              onChange={(event) =>
                setValues((current) => ({
                  ...current,
                  apartmentId: event.target.value,
                }))
              }
            >
              <option value="">Selectați apartamentul</option>
              {apartments.map((apartment) => (
                <option key={apartment.id} value={apartment.id}>
                  {apartment.building} · {apartment.apartment}
                </option>
              ))}
            </select>
            {renderFieldError("apartmentId")}
          </div>

          {mode === "create" ? (
            <>
              <div className="resident-dialog-field">
                <label htmlFor="temporaryPassword">Parolă temporară</label>
                <input
                  id="temporaryPassword"
                  name="temporaryPassword"
                  type="password"
                  autoComplete="new-password"
                  value={temporaryPassword}
                  minLength={15}
                  maxLength={128}
                  required
                  disabled={isSubmitting}
                  aria-invalid={
                    fieldErrors.temporaryPassword ? true : undefined
                  }
                  aria-describedby={fieldDescription("temporaryPassword")}
                  onChange={(event) => setTemporaryPassword(event.target.value)}
                />
                {renderFieldError("temporaryPassword")}
              </div>

              <div className="resident-dialog-field">
                <label htmlFor="confirmPassword">
                  Confirmă parola temporară
                </label>
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type="password"
                  autoComplete="new-password"
                  value={confirmPassword}
                  minLength={15}
                  maxLength={128}
                  required
                  disabled={isSubmitting}
                  aria-invalid={fieldErrors.confirmPassword ? true : undefined}
                  aria-describedby={fieldDescription("confirmPassword")}
                  onChange={(event) => setConfirmPassword(event.target.value)}
                />
                {renderFieldError("confirmPassword")}
              </div>
            </>
          ) : (
            <div className="resident-dialog-field">
              <label htmlFor="isActive">Stare cont</label>
              <select
                id="isActive"
                name="isActive"
                value={String(values.isActive)}
                disabled={isSubmitting}
                onChange={(event) =>
                  setValues((current) => ({
                    ...current,
                    isActive: event.target.value === "true",
                  }))
                }
              >
                <option value="true">Activ</option>
                <option value="false">Inactiv</option>
              </select>
            </div>
          )}

          <footer className="resident-dialog-actions">
            <button
              className="resident-dialog-button resident-dialog-button--secondary"
              type="button"
              disabled={isSubmitting}
              onClick={closeDialog}
            >
              Anulează
            </button>
            <button
              className="resident-dialog-button resident-dialog-button--primary"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting
                ? "Se salvează…"
                : mode === "create"
                  ? "Salvează locatarul"
                  : "Salvează modificările"}
            </button>
          </footer>
        </form>
      </div>
    </div>
  );
}

export default function ResidentFormDialog(props: ResidentFormDialogProps) {
  if (!props.open) return null;

  return (
    <ResidentFormDialogContent
      mode={props.mode}
      apartments={props.apartments}
      resident={props.resident}
      onSubmit={props.onSubmit}
      onClose={props.onClose}
    />
  );
}
