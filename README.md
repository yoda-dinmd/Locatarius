# Locatarius

> **Secure Condominium Governance & Administration Platform**  
> Aligned with Republic of Moldova Law 187/2022 (*Legea cu privire la condominiu*).

[![Security Focus](https://img.shields.io/badge/Security-DSA%20Standard-blue.svg)](#architectural-overview)

---

## 1. Project Overview
Locatarius addresses administrative opacity, unauthorized takeovers, and communication breakdowns during Moldova's transition from legacy municipal housing structures (ÎMGFL/JEC) to Homeowners' Associations (**APC** - *Asociație de Proprietari din Condominiu*).

### Roadmap Overview
- **Sprint 1 (Current Focus):** Authentication, Role-Based Access Control (RBAC), and user provisioning.
- **Sprints 2-3 (September MVP Target):** Apartment Directory, incident and maintenance ticketing with secure photo upload, and tamper-evident audit logging.
- **October-December (PBL Continuation):** Quorum verification, General Assembly voting, and repair and development fund ledger.

---

## 2. Sprint 1 Scope: Authentication & User Management

### Feature: Authentication & User Management

#### User Story 1: Administrator can authenticate
- User schema and initial admin seeding.
- Password hashing (Argon2 or BCrypt) and verification.
- Login API endpoint returning a JWT with role claims.
- Frontend login view and session persistence.

#### User Story 2: Administrator can create users
- Admin-only user creation endpoint (`[Authorize(Roles = "Admin")]`).
- Server-side and client-side validation.
- Add User UI form with role selection (`Admin`, `Owner`, `Tenant`).

#### User Story 3: Registered user can authenticate
- Tenant and Owner login flow.
- Route protection through backend policy gates and frontend protected routes.
- Error feedback for invalid credentials or unauthorized access.

---

## 3. Tech Stack

| Component | Technology | Rationale |
| :--- | :--- | :--- |
| **Backend** | ASP.NET Core (.NET 8 Web API) | Enterprise security middleware, native RBAC policies, and EF Core type-safety. |
| **Frontend** | React / Next.js + Tailwind CSS | Mobile-first responsive UI, PWA capability, and rapid component prototyping. |
| **Database** | PostgreSQL 16 | Relational data integrity for user roles and condominium entities. |
| **Security** | Argon2 / BCrypt, JWT Bearer | Secure credential storage, short-lived tokens, and protection against common OWASP risks. |
| **DevOps** | Docker Compose | Reproducible local development across operating systems. |

---

## 4. Quick Start (Local Environment)

### Prerequisites
- Docker and Docker Compose.
- Git.
- .NET 8 SDK for local backend development.
- Node.js 18 or later for local frontend development.

### Running the Database
```bash
# 1. Clone repository
git clone https://github.com/yoda-dinmd/Locatarius.git
cd Locatarius

# 2. Checkout the active sprint branch
git checkout feat/auth-user-mgmt

# 3. Start local PostgreSQL
docker compose up -d postgres
```

At this stage, start only the `postgres` service. The backend service will be enabled after the .NET solution and `backend/Dockerfile` are added.

- **PostgreSQL Port:** `localhost:5432`
- **Database:** `locatarius_db`
- **User:** `locatarius_admin`
- **Password:** `dev_secure_password_123` (development only)

## 5. Architectural Overview

The initial implementation is organized around identity and access management:

- **Presentation:** API Controllers (`AuthController`, `UsersController`), JWT middleware, exception handling, rate limiting, and Swagger.
- **Application:** User DTOs, authentication commands (`Login`, `CreateUser`), and input validation.
- **Domain:** `User` and `Role` entities, domain exceptions, and authentication rules.
- **Infrastructure:** `ApplicationDbContext` (EF Core), PostgreSQL migrations, `JwtTokenService`, and `PasswordHasher`.
