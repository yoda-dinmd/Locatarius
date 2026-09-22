// Numeric values match the backend IssueStatus and IssuePriority enums.
export type IssueStatus = 1 | 2 | 3;
export type IssuePriority = 1 | 2 | 3 | 4;

export type Issue = {
  issueId: string;
  reference: string;
  buildingId: string;
  title: string;
  description: string;
  reportedBy: string;
  reporterName: string;
  apartment: string;
  location: string;
  status: IssueStatus;
  priority: IssuePriority;
  createdAt: string;
  updatedAt: string | null;
  resolvedAt: string | null;
  attachments: { attachmentId: string; fileUrl: string; fileType: string; alt: string }[];
};

export const statuses = [1, 2, 3] as const;
export const statusLabels = { 1: "Open", 2: "In Progress", 3: "Closed" } as const;
export const priorityLabels = { 1: "Low", 2: "Medium", 3: "High", 4: "Critical" } as const;

// Keep transitions sequential, including when events arrive with stale state.
export function advanceIssue(issues: Issue[], id: string, expected: IssueStatus, now: string): Issue[] {
  return issues.map((issue) => {
    if (issue.issueId !== id || issue.status !== expected || expected === 3) return issue;
    const status = expected === 1 ? 2 : 3;
    return { ...issue, status, updatedAt: now, resolvedAt: status === 3 ? now : null };
  });
}

export function reprioritizeIssue(issues: Issue[], id: string, priority: IssuePriority, now: string): Issue[] {
  return issues.map((issue) => issue.issueId === id && issue.status !== 3 && issue.priority !== priority
    ? { ...issue, priority, updatedAt: now }
    : issue);
}
