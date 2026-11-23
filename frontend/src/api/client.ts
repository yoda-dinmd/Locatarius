/**
 * Shared API client (#77).
 *
 * Every backend call goes through `apiRequest`, which:
 * - always sends the HttpOnly session cookie (`credentials: "same-origin"`);
 * - attaches `X-CSRF-TOKEN` to unsafe methods, fetching the token from
 *   `GET /api/auth/csrf` on first use and keeping it in module memory only;
 * - retries an unsafe request once with a fresh token on `403 CSRF_INVALID`;
 * - decodes non-2xx responses into a typed `ApiError`.
 *
 * Nothing is written to localStorage, sessionStorage or JS-readable cookies.
 */

export type ApiErrorFields = Record<string, string[]>;

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly fields: ApiErrorFields;

  constructor(
    status: number,
    code: string,
    message: string,
    fields: ApiErrorFields = {},
  ) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.fields = fields;
  }
}

export type ApiRequestOptions = Omit<
  RequestInit,
  "body" | "credentials" | "headers"
> & {
  /** Serialized as JSON; strings are sent unchanged (already serialized). */
  body?: unknown;
  headers?: Record<string, string>;
};

const CSRF_PATH = "/api/auth/csrf";
const CSRF_HEADER = "X-CSRF-TOKEN";
const UNSAFE_METHODS = new Set(["POST", "PUT", "PATCH", "DELETE"]);

let csrfToken: string | null = null;
let pendingCsrfToken: Promise<string> | null = null;

/** Forget the cached CSRF token (e.g. after login/logout rotates the session). */
export function clearCsrfToken(): void {
  csrfToken = null;
  pendingCsrfToken = null;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function toFields(value: unknown): ApiErrorFields {
  if (!isRecord(value)) {
    return {};
  }

  const fields: ApiErrorFields = {};
  for (const [name, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) {
      fields[name] = messages.filter(
        (message): message is string => typeof message === "string",
      );
    }
  }
  return fields;
}

async function readJson(response: Response): Promise<unknown> {
  const contentType = response.headers.get("Content-Type") ?? "";
  if (response.status === 204 || !contentType.includes("application/json")) {
    return undefined;
  }

  try {
    return await response.json();
  } catch {
    return undefined;
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  const payload = await readJson(response);
  const error = isRecord(payload) ? payload.error : undefined;

  if (isRecord(error)) {
    return new ApiError(
      response.status,
      typeof error.code === "string" ? error.code : `HTTP_${response.status}`,
      typeof error.message === "string"
        ? error.message
        : "The request could not be completed.",
      toFields(error.fields),
    );
  }

  return new ApiError(
    response.status,
    `HTTP_${response.status}`,
    "The request could not be completed.",
  );
}

async function fetchCsrfToken(): Promise<string> {
  const response = await fetch(CSRF_PATH, {
    credentials: "same-origin",
    headers: { Accept: "application/json" },
  });

  if (!response.ok) {
    throw await toApiError(response);
  }

  const payload = await readJson(response);
  const token = isRecord(payload) ? payload.token : undefined;

  if (typeof token !== "string" || token.length === 0) {
    throw new ApiError(
      response.status,
      "CSRF_UNAVAILABLE",
      "Security token is unavailable. Refresh the page and try again.",
    );
  }

  return token;
}

async function getCsrfToken(): Promise<string> {
  if (csrfToken) {
    return csrfToken;
  }

  // Share one in-flight request between concurrent unsafe calls.
  pendingCsrfToken ??= fetchCsrfToken();
  const request = pendingCsrfToken;

  try {
    const token = await request;
    // Ignore the result if the cache was cleared while the request was in flight.
    if (pendingCsrfToken === request) {
      csrfToken = token;
    }
    return token;
  } finally {
    if (pendingCsrfToken === request) {
      pendingCsrfToken = null;
    }
  }
}

async function send(
  path: string,
  options: ApiRequestOptions,
  method: string,
  unsafe: boolean,
): Promise<Response> {
  const { body, headers: extraHeaders, ...init } = options;
  const headers: Record<string, string> = {
    Accept: "application/json",
    ...extraHeaders,
  };

  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
  }

  if (unsafe) {
    headers[CSRF_HEADER] = await getCsrfToken();
  }

  return fetch(path, {
    ...init,
    method,
    credentials: "same-origin",
    headers,
    body:
      body === undefined
        ? undefined
        : typeof body === "string"
          ? body
          : JSON.stringify(body),
  });
}

export async function apiRequest<T>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<T> {
  const method = (options.method ?? "GET").toUpperCase();
  const unsafe = UNSAFE_METHODS.has(method);

  let response = await send(path, options, method, unsafe);

  if (!response.ok) {
    let error = await toApiError(response);

    // The cached token is stale: refresh it and retry exactly once.
    if (unsafe && error.status === 403 && error.code === "CSRF_INVALID") {
      clearCsrfToken();
      response = await send(path, options, method, unsafe);
      if (!response.ok) {
        error = await toApiError(response);
      }
    }

    if (!response.ok) {
      throw error;
    }
  }

  return (await readJson(response)) as T;
}
