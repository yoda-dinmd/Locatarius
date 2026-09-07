# Locatarius

> **Secure Condominium Governance & Administration Platform**  
> Aligned with Republic of Moldova Law 187/2022 (*Legea cu privire la condominiu*).

[![Security Focus](https://img.shields.io/badge/Security-DSA%20Standard-blue.svg)](#security-architecture)

---

## 1. Project Overview
Locatarius addresses administrative opacity, unauthorized takeovers, and communication breakdowns during Moldova's transition from legacy municipal housing structures (ÎMGFL/JEC) to Homeowners' Associations (**APC** - *Asociație de Proprietari din Condominiu*).

### Sprint 1 & September Scope (Internship MVP)
- Multi-Tenant Authentication & Role-Based Access Control (RBAC): Tenant, Owner, Administrator.
- Condominium Structure & Apartment Directory.
- Verified Incident & Maintenance Ticketing (with secure photo upload).
- Tamper-evident Audit Logging for all administrative actions.

---

## 2. Tech Stack

| Component | Technology | Rationale |
| :--- | :--- | :--- |
| **Backend** | ASP.NET Core (.NET 8 Web API) | Native RBAC policies, EF Core type-safety, built-in protection against common web vulnerabilities. |
| **Frontend** | React / Next.js + Tailwind CSS | Mobile-first responsive UI, fast rendering, zero-native-build PWA capability. |
| **Database** | PostgreSQL 16 | Relational data integrity for association-to-unit ownership hierarchies. |
| **Security** | Argon2/Bcrypt, JWT Bearer | Secure authentication, credential hashing, and authorization guards. |
| **DevOps** | Docker Compose | Reproducible local development across all operating systems. |

---

## 3. Quick Start (Local Environment)

### Prerequisites
- Docker & Docker Compose
- Git

### Running Locally
```bash
# 1. Clone repository
git clone https://github.com/organization/locatarius.git
cd locatarius

# 2. Checkout the active sprint branch
git checkout feat/auth-user-mgmt

# 3. Start services
docker compose up -d
```

- **Backend API & Swagger:** `http://localhost:8080/swagger`
- **PostgreSQL Port:** `localhost:5432`

## 4. Architecture Blueprint (4-View Model)

### 1. Use View

- **Tenant (*Chiraș*):** Submits maintenance tickets, tracks repair statuses, views public building notices.
- **Owner (*Proprietar*):** Holds legal property verification, participates in assemblies, audits common repair fund usage.
- **Administrator (*Gestionar*):** Dispatches repair tickets, updates statuses, manages resident directories.

### 2. Functional View

- **Identity & Access Management (IAM):** Token issuance, password hashing, claims-based role validation.
- **Condominium Directory:** Hierarchical mapping (`Association` → `Building` → `Staircase` → `Apartment`).
- **Ticketing & Incident Management:** State machine (`OPEN` → `IN_PROGRESS` → `RESOLVED`).
- **Audit Logging Subsystem:** Append-only database logs tracking critical administrative actions.

### 3. Organic View (Clean Architecture Layers)

- **Presentation:** API Controllers, Middleware (JWT, Error Handling, Rate Limiting), Swagger.
- **Application:** CQRS / Application Services, DTOs, Input Validation.
- **Domain:** Entities (`User`, `Role`, `Condominium`, `Apartment`, `Ticket`), Enums, Domain Exceptions.
- **Infrastructure:** `ApplicationDbContext` (EF Core), PostgreSQL migrations, JWT Token Service.

### 4. Deployment View

- Single-command orchestration via `docker-compose.yml`.
- HTTPS exposure via local tunnels (`cloudflared` / `ngrok`) for remote mentor demonstrations.
