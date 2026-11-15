# Contributing to Locatarius

## 1. Git Branching Rules

- Keep `main` as the only long-lived integration branch. Do not push directly to `main`.

- Create each task branch from the current `main` branch:
  - `feat/<area>-<short-description>`
  - `fix/<area>-<short-description>`
  - `test/<area>-<short-description>`

- Submit Pull Requests (PRs) targeting `main` once the task is complete.

- Delete the task branch after the PR is merged. Keep local and remote branch lists
  pruned so they reflect the current repository state.

---

## 2. Commit Convention

Follow Conventional Commits and append the OpenProject work package reference:

```text
<type>(<scope>): <description> #<WP-ID>

```

- `feat`: New capability or endpoint

- `fix`: Bug fix

- `sec`: Security controls, RBAC, input sanitization

- `test`: Automated unit or integration tests

- `chore`: Tooling, Docker, or dependency configurations

_Example:_ `feat(auth): implement login endpoint and session creation #62`

---

## 3. Pull Requests & Review Checklist

### 3.1 PR Title Pattern

All PR titles must strictly follow the work package pattern to maintain bidirectional traceability with OpenProject:

```text
[#<WP-ID>] <type>(<scope>): <short description>

```

_Examples:_

- `[#62] feat(auth): add ASP.NET Core login endpoint and secure session creation`
- `[#63] feat(users): add admin-guarded user creation endpoint and conflict validation`
- `[#64] test(auth): add integration tests for resident role authorization boundaries`

---

### 3.2 PR Description Template

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

### 3.3 Review & Merge Gate

Before merging into `main`:

- [ ] Code builds cleanly in the CI pipeline without errors or warnings.

- [ ] At least one peer review approval has been submitted.

- [ ] Pull requests are squash-merged to maintain a clean git history.
