import { useEffect, useRef, useState } from "react";
import type { SubmitEvent } from "react";
import { ApiError } from "../api/client";
import { login } from "../auth/auth";
import "../styles/LoginPage.css";

type LoginField = "email" | "password";
type FieldErrors = Partial<Record<LoginField, string[]>>;

const RATE_LIMITED_MESSAGE = "Too many requests. Try again later.";
const INVALID_CREDENTIALS_MESSAGE = "Email or password is incorrect.";
const CONNECTION_MESSAGE =
	"Unable to connect to Locatarius. Check your connection and try again.";

function pickFieldErrors(fields: Record<string, string[]>): FieldErrors {
	const result: FieldErrors = {};
	if (fields.email?.length) result.email = fields.email;
	if (fields.password?.length) result.password = fields.password;
	return result;
}

export default function LoginPage() {
	const [showPassword, setShowPassword] = useState(false);
	const [error, setError] = useState("");
	const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
	const [isSubmitting, setIsSubmitting] = useState(false);
	const errorRef = useRef<HTMLParagraphElement>(null);
	const emailRef = useRef<HTMLInputElement>(null);
	const passwordRef = useRef<HTMLInputElement>(null);

	// Move focus to the first problem so keyboard and screen-reader users hear it.
	useEffect(() => {
		if (fieldErrors.email) {
			emailRef.current?.focus();
		} else if (fieldErrors.password) {
			passwordRef.current?.focus();
		} else if (error) {
			errorRef.current?.focus();
		}
	}, [error, fieldErrors]);

	function showLoginError(caught: unknown) {
		if (!(caught instanceof ApiError)) {
			setError(CONNECTION_MESSAGE);
			return;
		}

		if (caught.status === 400 && caught.code === "VALIDATION_FAILED") {
			const fields = pickFieldErrors(caught.fields);
			setFieldErrors(fields);
			// Fall back to the banner if the backend named no known field.
			if (!fields.email && !fields.password) {
				setError(caught.message);
			}
			return;
		}

		if (caught.status === 401 && caught.code === "INVALID_CREDENTIALS") {
			// Never keep a rejected password in the form.
			if (passwordRef.current) {
				passwordRef.current.value = "";
			}
			setShowPassword(false);
			setError(INVALID_CREDENTIALS_MESSAGE);
			return;
		}

		if (caught.status === 429 || caught.code === "RATE_LIMITED") {
			setError(RATE_LIMITED_MESSAGE);
			return;
		}

		setError(caught.message || CONNECTION_MESSAGE);
	}

	async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
		event.preventDefault();

		if (isSubmitting) {
			return;
		}

		setError("");
		setFieldErrors({});
		setIsSubmitting(true);

		const form = new FormData(event.currentTarget);

		try {
			const result = await login(
				String(form.get("email") ?? ""),
				String(form.get("password") ?? ""),
			);

			if (result.next === "change_password") {
				window.location.assign("/change-password");
				return;
			}

			window.location.assign("/dashboard");
		} catch (caught) {
			showLoginError(caught);
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
								ref={emailRef}
								id="email"
								name="email"
								type="email"
								autoComplete="email"
								required
								aria-invalid={fieldErrors.email ? true : undefined}
								aria-describedby={fieldErrors.email ? "email-error" : undefined}
							/>

							{fieldErrors.email && (
								<p id="email-error" className="error-message">
									{fieldErrors.email.join(" ")}
								</p>
							)}

							<label htmlFor="password">Password</label>

							<div className="password-field">
								<input
									ref={passwordRef}
									id="password"
									name="password"
									type={showPassword ? "text" : "password"}
									autoComplete="current-password"
									required
									aria-invalid={fieldErrors.password ? true : undefined}
									aria-describedby={
										fieldErrors.password ? "password-error" : undefined
									}
								/>

								<button
									className="show-password"
									type="button"
									onClick={() => setShowPassword((value) => !value)}
								>
									{showPassword ? "Hide" : "Show"}
								</button>
							</div>

							{fieldErrors.password && (
								<p id="password-error" className="error-message">
									{fieldErrors.password.join(" ")}
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
