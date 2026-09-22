// Numeric values match the implemented backend IssueStatus and IssuePriority enums.
export type IssueStatus = 1 | 2 | 3;
export type IssuePriority = 1 | 2 | 3 | 4;

export type Issue = {
  issueId: string;
  reportedBy: string;
  buildingId: string;
  title: string;
  description: string;
  status: IssueStatus;
  priority: IssuePriority;
  createdAt: string;
  updatedAt: string | null;
  resolvedAt: string | null;
  // Reporter and apartment are read through the implemented entity relationships.
  reporter: {
    userId: string;
    firstName: string;
    lastName: string;
    apartment: { apartmentNumber: string } | null;
  };
  attachments: {
    attachmentId: string;
    issueId: string;
    uploadedBy: string;
    fileUrl: string;
    fileType: string;
    createdAt: string;
  }[];
};

export const statuses = [1, 2, 3] as const;
export const statusLabels = { 1: "Open", 2: "In Progress", 3: "Closed" } as const;
export const priorityLabels = { 1: "Low", 2: "Medium", 3: "High", 4: "Critical" } as const;

export function issueLabel(issue: Issue): string {
  return `#${issue.issueId.slice(0, 8).toUpperCase()}`;
}

export function reporterName(issue: Issue): string {
  return `${issue.reporter.firstName} ${issue.reporter.lastName}`;
}

// Local demo state mirrors the EF Issue fields. Reopening clears ResolvedAt.
export function moveIssue(issues: Issue[], id: string, status: IssueStatus, now: string): Issue[] {
  return issues.map((issue) => issue.issueId === id && issue.status !== status
    ? { ...issue, status, updatedAt: now, resolvedAt: status === 3 ? now : null }
    : issue);
}

export function reprioritizeIssue(issues: Issue[], id: string, priority: IssuePriority, now: string): Issue[] {
  return issues.map((issue) => issue.issueId === id && issue.status !== 3 && issue.priority !== priority
    ? { ...issue, priority, updatedAt: now }
    : issue);
}
