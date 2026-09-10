# User stories and use cases

The seven use cases below define the application workflows. Each includes a user story, actors, preconditions, main and alternative flows, postconditions, API and data references, acceptance criteria and a sequence diagram.

| Use case | Phase / work package | Actors |
| --- | --- | --- |
| [UC-01 — Sign in and sign out](use-cases/UC-01.md) | Phase 1; #62 and #64 | Existing user |
| [UC-02 — Manage resident accounts](use-cases/UC-02.md) | Phase 1 creation (#63); Sprint 2 access management | Association admin |
| [UC-03 — Own profile, password and association access](use-cases/UC-03.md) | Phase 1 password lifecycle for #62/#64; profile editing in Sprint 2 | Authenticated user |
| [UC-04 — Maintain buildings, units and current residents](use-cases/UC-04.md) | Phase 2; tracker ID unassigned | Association admin manages assignments; either role reads own units |
| [UC-05 — Report and read private tickets](use-cases/UC-05.md) | Phase 2; tracker ID unassigned | Assigned occupant with either role |
| [UC-06 — Discuss and resolve tickets](use-cases/UC-06.md) | Phase 2; tracker ID unassigned | Ticket reporter and association admin |
| [UC-07 — Complete MFA and provider sign-in](use-cases/UC-07.md) | Phase 2; tracker ID unassigned | Existing user; operator for verified recovery/linking |

Phase 1 depends only on association/admin seed data and five foundation tables. Phase 2 dependencies: UC-04 → UC-05 → UC-06; UC-07 builds on UC-01/03 and can proceed independently of tickets. All phase 2 flows must use UC-07 full sessions by final delivery.

Confirmed story mapping and task-level delivery are listed in the [internship backlog](internship-backlog.md). Phase 2 work-package IDs are unassigned. See the [roadmap](mvp-roadmap.md#out-of-scope) for the scope boundary.
