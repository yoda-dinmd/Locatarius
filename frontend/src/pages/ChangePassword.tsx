import { useEffect, useRef, useState } from "react";
import type { SubmitEvent } from "react";
import { ApiError } from "../api/client";
import { changePassword, signOut } from "../auth/auth";
import "../styles/ChangePassword.css";

type PasswordField = "newPassword" | "confirmPassword";
type FieldErrors = Partial<Record<PasswordField, string[]>>;

const CONNECTION_MESSAGE =
  "Unable to save the password. Check your connection and try again.";

function pickFieldErrors(fields: Record<string, string[]>): FieldErrors {
  const result: FieldErrors = {};
  if (fields.newPassword?.length) result.newPassword = fields.newPassword;
  if (fields.confirmPassword?.length) {
    result.confirmPassword = fields.confirmPassword;
  }
  return result;
}

// App.tsx renders this page only for a server identity with mustChangePassword.
export default function ChangePassword() {
  const [showNew, setShowNew] = useState(false);
  const [showConfirmation, setShowConfirmation] = useState(false);
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [message, setMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isPasswordChanged, setIsPasswordChanged] = useState(false);
  const [isSessionExpired, setIsSessionExpired] = useState(false);
  const [isLeaving, setIsLeaving] = useState(false);
  const errorRef = useRef<HTMLParagraphElement>(null);
  const newPasswordRef = useRef<HTMLInputElement>(null);
  const confirmPasswordRef = useRef<HTMLInputElement>(null);

  // Move focus to the first problem so keyboard and screen-reader users hear it.
  useEffect(() => {
    if (fieldErrors.newPassword) {
      newPasswordRef.current?.focus();
    } else if (fieldErrors.confirmPassword) {
      confirmPasswordRef.current?.focus();
    } else if (error) {
      errorRef.current?.focus();
    }
  }, [error, fieldErrors]);

  function showChangeError(caught: unknown) {
    if (!(caught instanceof ApiError)) {
      setError(CONNECTION_MESSAGE);
      return;
    }

    if (caught.status === 401) {
      // The 5-minute password-change session has expired.
      setIsSessionExpired(true);
      return;
    }

    if (caught.status === 400 && caught.code === "VALIDATION_FAILED") {
      const fields = pickFieldErrors(caught.fields);
      setFieldErrors(fields);
      // Fall back to the banner if the backend named no known field.
      if (!fields.newPassword && !fields.confirmPassword) {
        setError(caught.message);
      }
      return;
    }

    setError(caught.message || CONNECTION_MESSAGE);
  }

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSubmitting || isPasswordChanged) {
      return;
    }

    setError("");
    setFieldErrors({});
    setMessage("");
    setIsSubmitting(true);

    const form = new FormData(event.currentTarget);

    try {
      const result = await changePassword(
        String(form.get("newPassword") ?? ""),
        String(form.get("confirmPassword") ?? ""),
      );

      setIsPasswordChanged(true);
      setMessage("Password changed successfully.");

      if (result.next === "app") {
        window.setTimeout(() => {
          window.location.assign("/dashboard");
        }, 700);
      }
    } catch (caught) {
      showChangeError(caught);
    } finally {
      setIsSubmitting(false);
    }
  }

  // The restricted session would keep forcing this page, so end it first.
  async function handleBackToSignIn() {
    if (isLeaving) {
      return;
    }

    setIsLeaving(true);
    try {
      await signOut();
    } catch {
      // Navigate anyway; the server session expires within minutes.
    }
    window.location.assign("/");
  }

  if (isSessionExpired && !isPasswordChanged) {
    return (
      <main className="login-page">
        <section className="login-shell">
          <aside className="welcome-panel">
            <a className="brand" href="/">
              <span className="brand-mark">L</span>
              <span>Locatarius</span>
            </a>

            <div className="welcome-content">
              <h1>
                A little closer
                <br />
                to home.
              </h1>

              <p className="welcome-copy">
                One place to stay connected with your building
                <br />
                and the people who look after it.
              </p>
            </div>
          </aside>

          <section className="form-panel">
            <div className="form-wrapper">
              <h2>Session expired</h2>
              <p className="form-intro">
                Please return to the sign-in page.
              </p>

              <a className="back-to-login" href="/">
                Back to sign in
              </a>
            </div>
          </section>
        </section>
      </main>
    );
  }

  return (
    <main className="login-page">
      <section className="login-shell">
        <aside className="welcome-panel">
          <a className="brand" href="/">
            <span className="brand-mark">L</span>
            <span>Locatarius</span>
          </a>

          <div className="welcome-content">
            <h1>
              A little closer
              <br />
              to home.
            </h1>

            <p className="welcome-copy">
              One place to stay connected with your building
              <br />
              and the people who look after it.
            </p>
          </div>
        </aside>

        <section className="form-panel">
          <div className="form-wrapper">
            <p className="form-eyebrow">Account security</p>

            <h2 className="form-title">
              Choose your own
              <br />
              password
            </h2>

            <p className="form-intro form-intro-left">
              Replace your temporary password before continuing.
            </p>

            <form onSubmit={handleSubmit}>
              <label htmlFor="newPassword">New password</label>

              <div className="password-field">
                <input
                  ref={newPasswordRef}
                  id="newPassword"
                  name="newPassword"
                  type={showNew ? "text" : "password"}
                  autoComplete="new-password"
                  minLength={15}
                  maxLength={128}
                  required
                  aria-invalid={fieldErrors.newPassword ? true : undefined}
                  aria-describedby={
                    fieldErrors.newPassword
                      ? "newPassword-error newPassword-hint"
                      : "newPassword-hint"
                  }
                />

                <button
                  className="show-password"
                  type="button"
                  onClick={() => setShowNew((value) => !value)}
                >
                  {showNew ? "Hide" : "Show"}
                </button>
              </div>

              {fieldErrors.newPassword && (
                <p id="newPassword-error" className="error-message">
                  {fieldErrors.newPassword.join(" ")}
                </p>
              )}

              <p id="newPassword-hint" className="password-hint">
                Use 15-128 characters. Spaces and Unicode are allowed.
              </p>

              <label htmlFor="confirmPassword">
                Confirm new password
              </label>

              <div className="password-field">
                <input
                  ref={confirmPasswordRef}
                  id="confirmPassword"
                  name="confirmPassword"
                  type={showConfirmation ? "text" : "password"}
                  autoComplete="new-password"
                  minLength={15}
                  maxLength={128}
                  required
                  aria-invalid={fieldErrors.confirmPassword ? true : undefined}
                  aria-describedby={
                    fieldErrors.confirmPassword
                      ? "confirmPassword-error"
                      : undefined
                  }
                />

                <button
                  className="show-password"
                  type="button"
                  onClick={() =>
                    setShowConfirmation((value) => !value)
                  }
                >
                  {showConfirmation ? "Hide" : "Show"}
                </button>
              </div>

              {fieldErrors.confirmPassword && (
                <p id="confirmPassword-error" className="error-message">
                  {fieldErrors.confirmPassword.join(" ")}
                </p>
              )}

              {error && (
                <p
                  ref={errorRef}
                  className="error-message"
                  role="alert"
                  aria-live="assertive"
                  tabIndex={-1}
                >
                  {error}
                </p>
              )}

              {message && (
                <p className="success-message" role="status">
                  {message}
                </p>
              )}

              <button
                className="submit-button"
                type="submit"
                disabled={isSubmitting || isPasswordChanged}
                aria-busy={isSubmitting}
              >
                {isSubmitting ? "Changing password..." : "Change password"}
              </button>
            </form>

            <a
              className="back-to-login"
              href="/"
              onClick={(event) => {
                event.preventDefault();
                void handleBackToSignIn();
              }}
              aria-disabled={isLeaving || undefined}
            >
              Back to sign in
            </a>
          </div>
        </section>
      </section>
    </main>
  );
}