# Locatarius: secure building management

A third-year TUM **Development of Secure Applications (PBL)** project for five students. Target stack: **.NET 10, PostgreSQL 18, React**. The deliverable is a small working application with demonstrated security controls.

## Start here

1. [Scope and screens](solution-design.md): what the finished application does.
2. [Sprint 1 acceptance criteria](sprint-1.md): implement and test #62, #63 and #64.
3. [Database](data-model.md): ten exact application tables and executable PostgreSQL DDL.
4. [API contract](api-catalog.md): routes, payloads, status codes and error bodies.
5. [Use cases and user stories](use-case-catalog.md): seven detailed workflows.
6. [Two-phase roadmap](mvp-roadmap.md): September foundation, October–December extension.
7. [Security and assessment evidence](operations-and-testing.md): threats, tests, recovery and incident response.

[Shared implementation rules](shared-patterns.md) · [Traceability](traceability.md) · [Decisions](decisions.md) · [Sources](sources.md) · [Documentation validation](validation.md)

## Frontend reference

[Screen mockups](frontend-mockups.md) include an interactive prototype, desktop/mobile images, form states and work-package mapping.

## Scope boundary

**September:** users sign in and out, manage their profile and change their password. Administrators create resident accounts and enable or disable association access. Tests use two associations to verify data isolation.

**By December:** administrators maintain buildings, apartments and current resident assignments. Residents submit private tickets and exchange comments with administrators. Security work includes multi-factor authentication (MFA), provider sign-in using OAuth 2.0/OpenID Connect (OIDC), and assessment evidence.

**Outside this project:** meters, accounting/financial journals, privacy-case workflows, assemblies/voting, document versioning, temporal engines, uploads, notifications, work orders and reporting engines. They have no implementation specifications or reserved tables. Reconsider only after university delivery in a separate scope decision.

## Using this specification

Use the API contract and shared rules when implementing the acceptance criteria. Record implementation progress and test evidence in [traceability](traceability.md). [Internship backlog](internship-backlog.md) maps the three authentication stories to task descriptions and Sprint 2/3 work.
