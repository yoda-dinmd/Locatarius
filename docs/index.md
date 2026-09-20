# Locatarius: secure building management

A third-year TUM **Development of Secure Applications (PBL)** project for five students. Target stack: **.NET 10, PostgreSQL 18, React**. The deliverable is a small working application with demonstrated security controls.

## Start here

1. [Scope and screens](solution-design.md): what the finished application does.
2. [Internship acceptance criteria](sprint-1.md): implement and test #62, #63 and #64.
3. [Database](data-model.md): eleven implemented application tables and migration-generated PostgreSQL DDL.
4. [API contract](api-catalog.md): routes, payloads, status codes and error bodies.
5. [Use cases and user stories](use-case-catalog.md): seven detailed workflows.
6. [Two-phase roadmap](mvp-roadmap.md): September foundation, October–December extension.
7. [Security and assessment evidence](operations-and-testing.md): threats, tests, recovery and incident response.

[Shared implementation rules](shared-patterns.md) · [Traceability](traceability.md) · [Decisions](decisions.md) · [Sources](sources.md) · [Documentation validation](validation.md)

## Local development

Use the [HTTPS/container runbook](local-https.md) for setup and the real-backend smoke
demo. The React auth flow remains a mock; see the [backlog](internship-backlog.md)
for application dependencies and task closure guidance.

## Frontend reference

[Screen mockups](frontend-mockups.md) include an interactive prototype, desktop/mobile images, form states and work-package mapping.

## Scope boundary

**Remaining internship:** finish #62–#64 and tasks #73–#90: sign-in/out, resident creation, first-login password change, authorization, database follow-ups and verification. Account lists, access-management features and profile editing are deferred until after the internship. Association-isolation requirements remain in scope.

**By December:** administrators maintain buildings, apartments and current resident assignments. Residents submit private tickets and exchange comments with administrators. Security work includes multi-factor authentication (MFA), provider sign-in using OAuth 2.0/OpenID Connect (OIDC), and assessment evidence.

**Outside this project:** meters, accounting/financial journals, privacy-case workflows, assemblies/voting, document versioning, temporal engines, uploads, notifications, work orders and reporting engines. They have no implementation specifications or reserved tables. Reconsider only after university delivery in a separate scope decision.

## Using this specification

Use the API contract and shared rules when implementing the acceptance criteria. Record implementation progress and test evidence in [traceability](traceability.md). [Internship backlog](internship-backlog.md) defines the current commitment and links the deferred backlog.
