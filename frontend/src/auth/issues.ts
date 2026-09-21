export type IssueStatus = "Open" | "In Progress" | "Closed";

export type IssueRecord = {
  id: string;
  residentId: string;
  buildingId: string;
  title: string;
  description: string;
  status: IssueStatus;
  evidenceName?: string;
  evidenceType?: string;
  createdAt: string;
  updatedAt: string;
};

export type IssueFilter = "All" | IssueStatus;

const ISSUES_KEY = "locatarius-mock-issues";

const initialIssues: IssueRecord[] = [
  {
    id: "ISS-1001",
    residentId: "resident-1",
    buildingId: "BLD-01",
    title: "Flickering hallway light",
    description:
      "The ceiling light in the main hallway flickers intermittently after 8 PM.",
    status: "Open",
    evidenceName: "hallway-light.jpg",
    evidenceType: "image/jpeg",
    createdAt: "2026-09-09T18:40:00.000Z",
    updatedAt: "2026-09-09T18:40:00.000Z",
  },
  {
    id: "ISS-1002",
    residentId: "resident-1",
    buildingId: "BLD-01",
    title: "Leaking pipe in bathroom",
    description:
      "There is a small leak under the sink that is causing water to collect on the cabinet floor.",
    status: "In Progress",
    evidenceName: "bathroom-leak.mp4",
    evidenceType: "video/mp4",
    createdAt: "2026-09-10T09:00:00.000Z",
    updatedAt: "2026-09-10T09:15:00.000Z",
  },
  {
    id: "ISS-1003",
    residentId: "resident-2",
    buildingId: "BLD-02",
    title: "Broken gate sensor",
    description:
      "The main entry gate sensor fails to detect vehicles during the evening shift.",
    status: "Closed",
    evidenceName: "gate-sensor.jpg",
    evidenceType: "image/jpeg",
    createdAt: "2026-09-04T14:10:00.000Z",
    updatedAt: "2026-09-05T11:30:00.000Z",
  },
  {
    id: "ISS-1004",
    residentId: "resident-3",
    buildingId: "BLD-03",
    title: "Noisy elevator",
    description:
      "The elevator rattles when reaching the 4th floor and has been making unusual noise during operation.",
    status: "Open",
    evidenceName: "elevator-noise.mp4",
    evidenceType: "video/mp4",
    createdAt: "2026-09-12T07:30:00.000Z",
    updatedAt: "2026-09-12T07:30:00.000Z",
  },
];

export function readIssues(): IssueRecord[] {
  const storedIssues = localStorage.getItem(ISSUES_KEY);

  if (!storedIssues) {
    localStorage.setItem(ISSUES_KEY, JSON.stringify(initialIssues));
    return initialIssues;
  }

  return JSON.parse(storedIssues) as IssueRecord[];
}

export function writeIssues(issues: IssueRecord[]) {
  localStorage.setItem(ISSUES_KEY, JSON.stringify(issues));
}

export function getFilterOptions(): IssueFilter[] {
  return ["All", "Open", "In Progress", "Closed"];
}

export function getIssuesForResident(residentId: string): IssueRecord[] {
  return readIssues()
    .filter((issue) => issue.residentId === residentId)
    .sort(
      (left, right) =>
        new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
    );
}

export function getAllIssues(): IssueRecord[] {
  return readIssues().sort(
    (left, right) =>
      new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime(),
  );
}

export function createIssue(input: {
  residentId: string;
  buildingId: string;
  title: string;
  description: string;
  evidenceName?: string;
  evidenceType?: string;
}): IssueRecord {
  const now = new Date().toISOString();

  const issue: IssueRecord = {
    id: `ISS-${Date.now()}`,
    residentId: input.residentId,
    buildingId: input.buildingId,
    title: input.title.trim(),
    description: input.description.trim(),
    status: "Open",
    evidenceName: input.evidenceName,
    evidenceType: input.evidenceType,
    createdAt: now,
    updatedAt: now,
  };

  const issues = readIssues();
  issues.unshift(issue);
  writeIssues(issues);

  return issue;
}
