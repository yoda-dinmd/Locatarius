import { type FormEvent, useState } from "react";
import "./App.css";

export default function App() {
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");

    const form = new FormData(event.currentTarget);

    const response = await fetch("/api/auth/login", {
      method: "POST",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        email: form.get("email"),
        password: form.get("password"),
      }),
    });

    if (!response.ok) {
      setError("Unable to sign in. Check your credentials.");
      return;
    }

    window.location.href = "/dashboard";
  }

  function signInWithGoogle() {
    window.location.href = "/api/auth/google/start";
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
            <h2>Welcome Back</h2>

            <p className="form-intro">
              Enter the credentials provided by your administrator.
            </p>

            <form onSubmit={handleSubmit}>
              <label htmlFor="email">Email address</label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                required
              />

              <label htmlFor="password">Password</label>

              <div className="password-field">
                <input
                  id="password"
                  name="password"
                  type={showPassword ? "text" : "password"}
                  autoComplete="current-password"
                  required
                />

                <button
                  className="show-password"
                  type="button"
                  onClick={() => setShowPassword((value) => !value)}
                >
                  {showPassword ? "Hide" : "Show"}
                </button>
              </div>

              {error && <p className="error-message">{error}</p>}

              <button className="submit-button" type="submit">
                Sign In
              </button>
            </form>

            <button
              className="google-button"
              type="button"
              onClick={signInWithGoogle}
            >
              <img
                className="google-icon"
                src="/google-g.svg"
                alt=""
                aria-hidden="true"
              />
              Sign In with Google
            </button>

            <p className="support-copy">
              Need access or help signing in?
              <br />
              Contact your association administrator.
            </p>
          </div>
        </section>
      </section>
    </main>
  );
}