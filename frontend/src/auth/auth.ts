import { ApiError, apiRequest, clearCsrfToken } from "../api/client";

/**
 * Server-backed authentication (#77).
 *
 * The session lives only in the backend's HttpOnly cookie. Identity is read
 * from `GET /api/auth/me` and kept in module memory; nothing is persisted in
 * localStorage or sessionStorage.
 */

export type UserRole = "admin" | "resident";

export type SessionUser = {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  mustChangePassword: boolean;
};

export type LoginResponse = { next: "app" | "change_password" };
export type ChangePasswordResponse = { next: "app" };

let currentIdentity: SessionUser | null = null;

function isSessionUser(value: unknown): value is SessionUser {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const user = value as Record<string, unknown>;
  return (
    typeof user.id === "string" &&
    typeof user.name === "string" &&
    typeof user.email === "string" &&
    (user.role === "admin" || user.role === "resident") &&
    typeof user.mustChangePassword === "boolean"
  );
}

/** Last identity resolved by `getIdentity()` in this page load, if any. */
export function getCurrentIdentity(): SessionUser | null {
  return currentIdentity;
}

/** Resolves the signed-in user, or `null` when there is no valid session. */
export async function getIdentity(): Promise<SessionUser | null> {
  try {
    const user = await apiRequest<unknown>("/api/auth/me");

    if (!isSessionUser(user)) {
      throw new ApiError(
        500,
        "INVALID_IDENTITY",
        "The server returned an invalid identity.",
      );
    }

    currentIdentity = user;
    return user;
  } catch (error) {
    currentIdentity = null;

    if (error instanceof ApiError && error.status === 401) {
      return null;
    }
    throw error;
  }
}

export async function login(
  email: string,
  password: string,
): Promise<LoginResponse> {
  currentIdentity = null;

  const response = await apiRequest<LoginResponse>("/api/auth/login", {
    method: "POST",
    body: { email, password },
  });

  // The session cookie was rotated; fetch a fresh CSRF token on next use.
  clearCsrfToken();
  return response;
}

export async function changePassword(
  newPassword: string,
  confirmPassword: string,
): Promise<ChangePasswordResponse> {
  const response = await apiRequest<ChangePasswordResponse>(
    "/api/auth/change-password",
    {
      method: "POST",
      body: { newPassword, confirmPassword },
    },
  );

  currentIdentity = null;
  clearCsrfToken();
  return response;
}

export async function signOut(): Promise<void> {
  try {
    await apiRequest<unknown>("/api/auth/logout", {
      method: "POST",
      body: {},
    });
  } catch (error) {
    // An already expired session is effectively signed out.
    if (!(error instanceof ApiError && error.status === 401)) {
      throw error;
    }
  } finally {
    currentIdentity = null;
    clearCsrfToken();
  }
}
