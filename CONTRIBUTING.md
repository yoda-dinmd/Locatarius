# Contributing to Locatarius



## 1. Scrum Workflow



* Sprints run in **1-week cycles** starting Mondays.


* Daily standups focus on: what was completed, what is planned today, and blockers.


* All team members must log working hours daily in OpenProject (**Time and costs** module).



---

## 2. Git Branching Rules



* **Never push directly to `main` or `develop`.**

* For Sprint 1, create task branches off `feat/auth-user-mgmt`:


* `feat/<area>-<short-description>`

* `fix/<area>-<short-description>`

* `test/<area>-<short-description>`



* Submit Pull Requests (PRs) targeting `feat/auth-user-mgmt`.



---

## 3. Commit Convention



Follow Conventional Commits and append the OpenProject work package reference:

```text
<type>(<scope>): <description> #<WP-ID>

```

* `feat`: New capability or endpoint


* `fix`: Bug fix


* `sec`: Security controls, RBAC, input sanitization


* `test`: Automated unit or integration tests


* `chore`: Tooling, Docker, or dependency configurations



*Example:* `feat(auth): implement login endpoint and JWT issuance #62`

---

## 4. Pull Requests & Review Checklist



### 4.1 PR Title Pattern

All PR titles must strictly follow the work package pattern to maintain bidirectional traceability with OpenProject:

```text
[#<WP-ID>] <type>(<scope>): <short description>

```

*Examples:*

* `[#62] feat(auth): add ASP.NET Core login endpoint and JWT bearer issuance`
* `[#63] feat(users): add admin-guarded user creation endpoint and conflict validation`
* `[#64] test(auth): add integration tests for resident role authorization boundaries`

---

### 4.2 PR Description Template

Every PR description must reference its parent story or task:

```markdown
## OpenProject Work Package
Resolves / Closes: #<WP-ID>

## Changes Proposed
- Bullet points summarizing the technical changes

## Acceptance Criteria & Security Checklist
- [ ] Meets user story acceptance criteria defined in OpenProject.
- [ ] Adheres to project tenancy and role authorization checks (AUTH-01 / ERR-01).
- [ ] No plain-text credentials, tokens, or connection strings committed.
- [ ] Code builds without errors (`dotnet build` / `npm run build`).
- [ ] Unit or integration tests pass locally.

## How to Test
1. Step-by-step reproduction instructions for the reviewer.

```

---

### 4.3 Review & Merge Gate



Before merging into `feat/auth-user-mgmt`:

* [ ] Code builds cleanly in the CI pipeline without errors or warnings.


* [ ] At least one peer review approval has been submitted.


* [ ] Pull requests are squash-merged to maintain a clean git history.
