/** Synthetic IssueDto as returned by /api/issues, for unit tests only. */
export function issueDto(overrides: Record<string, unknown> = {}) {
  return {
    issueId: "50000000-0000-0000-0000-000000000001",
    reportedBy: "20000000-0000-0000-0000-000000000001",
    buildingId: "10000000-0000-0000-0000-000000000001",
    buildingNumber: "A",
    title: "Broken entrance light",
    description: "The entrance light does not turn on.",
    status: 1,
    priority: 3,
    createdAt: "2026-11-20T10:00:00+00:00",
    updatedAt: null,
    resolvedAt: null,
    reporter: {
      userId: "20000000-0000-0000-0000-000000000001",
      firstName: "Ana",
      lastName: "Rusu",
      apartment: { apartmentNumber: "101" },
    },
    attachments: [],
    ...overrides,
  };
}

export function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

export function apiErrorResponse(
  status: number,
  code: string,
  message: string,
  fields: Record<string, string[]> = {},
): Response {
  return jsonResponse(status, { error: { code, message, fields } });
}
