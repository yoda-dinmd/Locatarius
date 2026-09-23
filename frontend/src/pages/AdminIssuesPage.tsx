import type { SessionUser } from "../auth/auth";
import IssueTriage from "../features/issue-triage/IssueTriage";
import IssueLayout from "./IssueLayout";

type AdminIssuesPageProps = {
  user: SessionUser;
};

export default function AdminIssuesPage({ user }: AdminIssuesPageProps) {
  return (
    <IssueLayout user={user} activePage="issues">
      <IssueTriage />
    </IssueLayout>
  );
}
