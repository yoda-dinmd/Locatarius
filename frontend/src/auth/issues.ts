import { ApiError, apiRequest } from "../api/client";
import type {
  Issue,
  IssuePriority,
  IssueStatus,
} from "../features/issue-triage/issueModel";

/**
 * Issues API facade (#118) over `/api/issues` (#117).
 *
 * Pages call these functions instead of building URLs, attaching CSRF tokens
 * or parsing JSON themselves. The backend decides which issues the caller may
 * see (administrators: their buildings; residents: their own building), so no
 * scoping happens here. Nothing is stored in browser storage.
 */

export type { Issue, IssuePriority, IssueStatus };

export type NewIssue = { title: string; description: string };
export type IssueChanges = { status?: IssueStatus; priority?: IssuePriority };

export const ISSUE_TITLE_MIN = 5;
export const ISSUE_TITLE_MAX = 120;
export const ISSUE_DESCRIPTION_MIN = 10;
export const ISSUE_DESCRIPTION_MAX = 2000;

const ISSUES_PATH = "/api/issues";
const GUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

type RequestOptions = { signal?: AbortSignal };

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

// Adapter guard: reject responses that do not match IssueDto instead of
// letting a malformed payload crash a page later during rendering.
function isIssue(value: unknown): value is Issue {
  if (!isRecord(value) || !isRecord(value.reporter)) return false;
  const apartment = value.reporter.apartment;
  return (
    typeof value.issueId === "string" &&
    typeof value.reportedBy === "string" &&
    typeof value.buildingId === "string" &&
    typeof value.buildingNumber === "string" &&
    typeof value.title === "string" &&
    typeof value.description === "string" &&
    (value.status === 1 || value.status === 2 || value.status === 3) &&
    [1, 2, 3, 4].includes(value.priority as number) &&
    typeof value.createdAt === "string" &&
    (value.updatedAt === null || typeof value.updatedAt === "string") &&
    (value.resolvedAt === null || typeof value.resolvedAt === "string") &&
    typeof value.reporter.userId === "string" &&
    typeof value.reporter.firstName === "string" &&
    typeof value.reporter.lastName === "string" &&
    (apartment === null ||
      (isRecord(apartment) && typeof apartment.apartmentNumber === "string")) &&
    Array.isArray(value.attachments)
  );
}

function invalidResponse(): ApiError {
  return new ApiError(
    500,
    "INVALID_ISSUE",
    "The server returned an unexpected issue format.",
  );
}

function toIssue(value: unknown): Issue {
  if (!isIssue(value)) throw invalidResponse();
  return value;
}

export function isIssueId(value: string): boolean {
  return GUID_PATTERN.test(value);
}

/** Issues visible to the signed-in user, newest first (server order). */
export async function listIssues(options: RequestOptions = {}): Promise<Issue[]> {
  const payload = await apiRequest<unknown>(ISSUES_PATH, { signal: options.signal });
  if (!Array.isArray(payload)) throw invalidResponse();
  return payload.map(toIssue);
}

/**
 * One issue, or `null` when it does not exist or is outside the caller's
 * buildings (the backend answers both with 404 ISSUE_NOT_FOUND).
 */
export async function getIssue(
  issueId: string,
  options: RequestOptions = {},
): Promise<Issue | null> {
  // Legacy mock IDs such as "ISS-1001" can never exist; skip the request.
  if (!isIssueId(issueId)) return null;
  try {
    const payload = await apiRequest<unknown>(
      `${ISSUES_PATH}/${encodeURIComponent(issueId)}`,
      { signal: options.signal },
    );
    return toIssue(payload);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

/**
 * Residents only. Reporter, building, status and timestamps are assigned by
 * the server, so only the title and description are sent.
 */
export async function createIssue(input: NewIssue): Promise<Issue> {
  const payload = await apiRequest<unknown>(ISSUES_PATH, {
    method: "POST",
    body: { title: input.title, description: input.description },
  });
  return toIssue(payload);
}

/** Administrators only: change status and/or priority. */
export async function updateIssue(
  issueId: string,
  changes: IssueChanges,
): Promise<Issue> {
  const body: IssueChanges = {};
  if (changes.status !== undefined) body.status = changes.status;
  if (changes.priority !== undefined) body.priority = changes.priority;
  const payload = await apiRequest<unknown>(
    `${ISSUES_PATH}/${encodeURIComponent(issueId)}`,
    { method: "PATCH", body },
  );
  return toIssue(payload);
}

/** User-facing message for any error thrown by the functions above. */
export function describeIssueError(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.code) {
      case "UNAUTHENTICATED":
        return "Your session has expired. Sign in again to continue.";
      case "ISSUE_NOT_FOUND":
        return "This issue no longer exists or is not available to you.";
      case "APARTMENT_NOT_FOUND":
        return "Your account is not assigned to an apartment yet, so you cannot report issues. Contact your administrator.";
      default:
        if (error.status >= 500) {
          return "The server could not complete the request. Try again later.";
        }
        return error.message;
    }
  }
  return "Unable to reach Locatarius. Check your connection and try again.";
}
