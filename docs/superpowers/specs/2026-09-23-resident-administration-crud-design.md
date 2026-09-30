# Resident Administration CRUD — Design

Date: 2026-09-23
Status: Approved in conversation; awaiting an OpenProject work package ID

## Context

The existing resident directory is a static frontend prototype. Its data comes
from `residentsMock.json`, authentication is simulated in `localStorage`, and
the action buttons only log events. The backend already has PostgreSQL-backed
users, credentials, sessions, apartments, buildings, and the security role
values `Admin` and `Resident`, but it has no resident-management endpoints,
identity endpoint, or real first-login password-change endpoint.

The latest OpenProject snapshot explicitly defers account listing and access
state management. The user has explicitly requested this new scope, but the
repository still requires a new work package ID before commits and
implementation can proceed.

## Goals

- Replace the resident directory's mock data with association-scoped database
  data.
- Make Add, Edit, Deactivate, and Reactivate persistent and usable end to end.
- Connect frontend authentication, logout, identity loading, and first-login
  password change to the existing backend session model.
- Preserve only `Admin` and `Resident` as authorization roles.
- Ensure administrators can access only residents and apartments belonging to
  buildings they administer.
- Store passwords only as salted hashes and keep resident-chosen passwords
  unknown to administrators.

## Non-goals

- Adding `Proprietar` or `Chiriaș` as roles or occupancy classifications.
- Hard-deleting resident records.
- Password reset by an administrator after initial account creation.
- Building, apartment, or administrator management.
- Sending temporary credentials by email or another messaging service.

## Domain and Persistence Model

`UserRoleType` remains unchanged with only `Resident` and `Admin`.

Add a required `User.IsActive` Boolean mapped to `users.is_active`, defaulting
to `true`. A migration sets existing users active. A resident's building scope
continues to be derived through `User.ApartmentId -> Apartment.BuildingId ->
Building.AdminId`; no new membership table is introduced in this change.

Deactivation sets `IsActive = false`, clears no historical data, and revokes
all sessions belonging to the resident. Reactivation sets it back to `true`.
Authentication treats an inactive account like invalid credentials and returns
the same generic response.

## Authorization Boundary

Every protected operation resolves the server-side session cookie. Business
operations require a non-expired full session. Resident-management operations
also require the authenticated user's database role to be `Admin`.

Resident and apartment queries include the building ownership condition in the
database query itself. A resident or apartment outside the current
administrator's buildings is treated as not found. The frontend route guard is
only presentation logic; the backend is the security boundary.

Restricted password-change sessions may call only the identity, logout, CSRF,
and change-password endpoints. They cannot call resident-management endpoints.

## API Contracts

### Authentication

- `GET /api/auth/csrf` keeps the existing contract.
- `POST /api/auth/login` keeps the existing contract and rejects inactive
  accounts with the generic invalid-credentials response.
- `GET /api/auth/me` returns the authenticated user's safe identity: ID,
  display name, email, role, and whether password change is required. It returns
  `401` for no valid session and records accepted activity only after successful
  session validation.
- `POST /api/auth/change-password` accepts `newPassword` and
  `confirmPassword`. It requires a valid password-change session, validates the
  shared password rules, replaces only the password hash, sets
  `MustChangePassword = false`, sets `PasswordChangedAt`, revokes the restricted
  session, and creates a full session atomically. It returns no password data.
- `POST /api/auth/logout` keeps the existing CSRF-protected behavior.

### Residents and Apartments

- `GET /api/residents` returns residents scoped to buildings owned by the
  current administrator. Each item contains `id`, `firstName`, `lastName`,
  `email`, `apartmentId`, `apartment`, `role: "Resident"`, and `isActive`.
- `GET /api/apartments` returns `id`, display number, and building context only
  for apartments in the administrator's buildings.
- `POST /api/residents` accepts `firstName`, `lastName`, `email`,
  `apartmentId`, `temporaryPassword`, and `confirmPassword`. It rejects unknown
  properties, derives the role as `Resident`, hashes the temporary password,
  sets `MustChangePassword = true`, and atomically creates the user,
  credentials, role, and apartment association. It returns `201` and a safe
  resident DTO.
- `PUT /api/residents/{id}` accepts `firstName`, `lastName`, `email`,
  `apartmentId`, and `isActive`. It never accepts a role or password. Changing
  `isActive` to false revokes the resident's sessions in the same transaction.
- `DELETE /api/residents/{id}` performs idempotent logical deactivation and
  revokes the resident's sessions. It never deletes the database row.

All unsafe endpoints require a valid CSRF token and JSON content type. Request
bodies use the existing 16 KiB maximum. Responses use the existing safe error
envelope. Expected status codes include `400` for validation or malformed
requests, `401` for invalid sessions, `403` for authenticated non-admin users,
`404` for inaccessible resources, and `409` for duplicate canonical email.

## Validation and Credential Handling

Names are trimmed, required, and constrained to the existing database maximum
of 100 characters each. Email is trimmed, canonicalized consistently with
login, limited to 254 characters, and globally unique. Apartment IDs must
belong to a building managed by the caller.

Temporary and new passwords use the shared password rules. Password values are
never logged, returned, placed in URLs, persisted in frontend storage, or shown
in success notifications. Form state containing credentials is cleared on
success and cancel. The administrator communicates the one-time temporary
password to the resident outside this system.

## Frontend Architecture and Flow

A shared API client obtains and refreshes the CSRF token, sends requests with
same-origin credentials, decodes the standard error envelope, and retries an
unsafe request once when the CSRF token is stale. It does not persist session
tokens or credentials.

Application bootstrap calls `GET /api/auth/me` and renders a loading state while
identity is resolved. Unauthenticated users see the login page. A successful
login follows the backend's `next` value. `change_password` routes to the
password-change screen; `app` reloads identity and routes to the dashboard.

The resident page loads residents and authorized apartments in parallel. It
shows a loading state, a directed empty state, or the table. The Role column
always displays `Resident`; Status displays the persisted active state.

`Adaugă locatar` opens an accessible modal containing first name, last name,
email, apartment, temporary password, and confirmation. `Editează` opens a
modal containing first name, last name, email, apartment, and active state, but
no password fields. `Șterge` opens a confirmation dialog and performs logical
deactivation. Successful mutations replace the affected table state from the
server response or reload the list. In-flight controls are disabled to prevent
duplicate submissions.

Field validation errors render beside their inputs. Authentication failures
return the user to login. Forbidden, not-found, conflict, and network failures
produce safe Romanian messages and preserve non-credential form data where it
is useful. Credential fields are always cleared.

## Reverse Proxy and Local Development

Production-style Docker access continues through the repository's top-level
NGINX, which already proxies `/api/` to the backend and all other paths to the
frontend. The frontend image's internal NGINX continues rejecting direct
`/api/` access so the backend is reached only through the trusted proxy.

Development configuration must provide a same-origin HTTPS path compatible
with the backend's `__Host-` secure cookies and CSRF policy. The preferred
end-to-end environment is the existing Docker Compose stack at the top-level
HTTPS origin; no insecure cookie fallback is added.

## Testing Strategy

- Unit tests cover request validation, active-account authentication behavior,
  password transition logic, and authorization decisions.
- PostgreSQL-backed integration tests cover the migration, canonical email
  uniqueness, atomic creation, apartment scoping, update, session revocation,
  deactivation, and reactivation.
- HTTP tests cover CSRF and the `200/201/400/401/403/404/409` contracts, plus
  cross-building isolation and resident-to-admin denial.
- Frontend tests cover identity bootstrap, login outcomes, populated and empty
  directories, forms, field errors, pending states, confirmation,
  deactivation, reactivation, and credential-state clearing.
- End-to-end verification follows: administrator login, resident creation,
  resident temporary login, mandatory password change, normal resident login,
  administrator edit, deactivation, rejected inactive login, and reactivation.
- Final verification includes backend tests, frontend tests, lint, production
  builds, database migration against PostgreSQL, and desktop/mobile browser
  checks.

## Delivery Constraint

No implementation commit may be created until a new OpenProject work package
exists and its ID is supplied. The commit and pull-request titles must follow
`CONTRIBUTING.md` and include that ID. The work package should explicitly
supersede the latest snapshot's deferral of account listing and access-state
management.
