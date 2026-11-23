import { useEffect, useRef, useState } from "react";
import type { SubmitEvent } from "react";
import { ApiError } from "../api/client";
import { changePassword } from "../auth/auth";
import "../styles/ChangePassword.css";

// App.tsx renders this page only for a server identity with mustChangePassword.
export default function ChangePassword() {
  const [showNew, setShowNew] = useState(false);
  const [showConfirmation, setShowConfirmation] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isPasswordChanged, setIsPasswordChanged] = useState(false);
  const [isSessionExpired, setIsSessionExpired] = useState(false);
  const errorRef = useRef<HTMLParagraphElement>(null);

  useEffect(() => {
    if (error) {
      errorRef.current?.focus();
    }
  }, [error]);

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    if (isSubmitting) {
      return;
    }

    setError("");
    setMessage("");
    setIsSubmitting(true);

    const form = new FormData(event.currentTarget);

    try {
      await changePassword(
        String(form.get("newPassword") ?? ""),
        String(form.get("confirmation") ?? ""),
      );

      setIsPasswordChanged(true);
      setMessage("Password changed successfully.");

      window.setTimeout(() => {
        window.location.assign("/dashboard");
      }, 700);
    } catch (caught) {
      // Detailed per-field error display is delivered by #76.
      if (caught instanceof ApiError && caught.status === 401) {
        // The 5-minute password-change session has expired.
        setIsSessionExpired(true);
        return;
      }
      if (caught instanceof ApiError) {
        const fieldMessage = Object.values(caught.fields).flat()[0];
        setError(fieldMessage ?? caught.message);
        return;
      }
      setError(
        "Unable to save the password. Check your connection and try again.",
      );
    } finally {
      setIsSubmitting(false);
    }
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
                  id="newPassword"
                  name="newPassword"
                  type={showNew ? "text" : "password"}
                  autoComplete="new-password"
                  minLength={15}
                  maxLength={128}
                  required
                />

                <button
                  className="show-password"
                  type="button"
                  onClick={() => setShowNew((value) => !value)}
                >
                  {showNew ? "Hide" : "Show"}
                </button>
              </div>

              <p className="password-hint">
                Use 15-128 characters. Spaces and Unicode are allowed.
              </p>

              <label htmlFor="confirmation">
                Confirm new password
              </label>

              <div className="password-field">
                <input
                  id="confirmation"
                  name="confirmation"
                  type={showConfirmation ? "text" : "password"}
                  autoComplete="new-password"
                  minLength={15}
                  maxLength={128}
                  required
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
                disabled={isSubmitting}
              >
                {isSubmitting ? "Changing password..." : "Change password"}
              </button>
            </form>

            <a className="back-to-login" href="/">
              Back to sign in
            </a>
          </div>
        </section>
      </section>
    </main>
  );
}