import { useEffect, useRef, useState } from "react";
import type { SubmitEvent } from "react";
import { login } from "../auth/auth";
import "../styles/LoginPage.css";

export default function LoginPage() {
	const [showPassword, setShowPassword] = useState(false);
	const [error, setError] = useState("");
	const [isSubmitting, setIsSubmitting] = useState(false);
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
		setIsSubmitting(true);

		const form = new FormData(event.currentTarget);

		try {
			const result = await login(
				String(form.get("email") ?? ""),
				String(form.get("password") ?? ""),
			);

			if (!result.ok) {
				setError(result.message);
				return;
			}

            if (result.next === "change_password") {
				window.location.assign("/change-password");
				return;
            }

            window.location.assign("/dashboard");
		} catch {
			setError(
				"Unable to connect to Locatarius. Check your connection and try again.",
			);
		} finally {
			setIsSubmitting(false);
		}
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

							<button
								className="submit-button"
								type="submit"
								disabled={isSubmitting}
								aria-busy={isSubmitting}
							>
								{isSubmitting ? "Signing In..." : "Sign In"}
							</button>

							<button
								className="google-button"
								type="button"
								disabled={isSubmitting}
							>
								<img
									className="google-icon"
									src="/google-g.svg"
									alt=""
									aria-hidden="true"
								/>
								Sign In with Google
							</button>
						</form>

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
