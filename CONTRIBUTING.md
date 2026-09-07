# Contributing to Locatarius

## 1. Scrum Workflow
- Sprints run in **1-week cycles** starting Mondays.
- Daily standups focus on: what was completed, what is planned today, and blockers.
- All team members must log working hours daily in OpenProject (**Time and costs** module).

---

## 2. Git Branching Rules
- **Never push directly to `main` or `develop`.**
- For Sprint 1, create task branches off `feat/auth-user-mgmt`:
  - `feat/<area>-<short-description>`
  - `fix/<area>-<short-description>`
  - `test/<area>-<short-description>`
- Submit Pull Requests (PRs) targeting `feat/auth-user-mgmt`.

---

## 3. Commit Convention
Follow Conventional Commits:
```text
<type>(<scope>): <description>
```

- `feat`: New capability or endpoint
- `fix`: Bug fix
- `sec`: Security controls, RBAC, input sanitization
- `test`: Automated unit or integration tests
- `chore`: Tooling, Docker, or dependency configurations

## 4. PR Checklist
Before requesting a review:

- [ ] Code builds without errors (`dotnet build` / `npm run build`).
- [ ] No hardcoded credentials or database secrets committed.
- [ ] User story acceptance criteria are met.
- [ ] At least one peer review approval.
