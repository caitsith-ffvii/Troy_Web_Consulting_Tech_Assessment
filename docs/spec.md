# Troy Web Consulting Technical Assessment — Specification & Progress

**Project:** Property Management Application  
**Stack:** .NET 10, ASP.NET Core MVC/Razor, EF Core, SQL Server, ASP.NET Core Identity, xUnit  
**Purpose:** Track assessment requirements, implementation status, test evidence, and remaining work.

## Status definitions

- [x] **Complete** — implemented and supported by relevant verification.
- [ ] **In progress** — partially implemented or missing some required coverage.
- [?] **Not verified** — implementation may exist, but compliance has not been confirmed.
- [ ] **Not started** — implementation remains to be done.

Do not mark a requirement complete just because code exists. Record test evidence where appropriate.

---

## 1. Foundation and architecture

- [x] .NET solution with Domain, Web, and Tests projects.
- [x] ASP.NET Core MVC/Razor application.
- [x] SQL Server and EF Core integration.
- [x] ASP.NET Core Identity foundation.
- [x] Separate domain entities, services, controllers, and view models.
- [x] Business rules enforced in services rather than relying exclusively on UI validation.
- [x] Service results represent expected business failures.
- [x] Async EF Core operations.
- [?] Review dependency injection, entity configuration, and project boundaries.
- [?] Confirm database migrations and clean-database setup work from documented instructions.

## 2. Identity, profiles, and authorization

- [x] Applicant profile linked to Identity user.
- [x] Property manager profile linked to Identity user.
- [x] Applicant-scoped application lookup.
- [x] Applicant edit operations verify ownership.
- [x] Manager status changes verify assigned property ownership.
- [x] Sign-up with role choice creates the Identity user, role, and profile atomically. Evidence: `RegistrationServiceTests`.
- [x] An existing account can add the other role by re-registering with its password. Evidence: `RegistrationServiceTests`; manual check.
- [x] Login and logout. Evidence: manual check, 2026-10-18.
- [x] Applicant pages require the Applicant role. Evidence: manual check (manager receives Access Denied).
- [x] Verify role and profile provisioning for newly registered users.
- [?] Verify role and profile provisioning for seeded users.
- [?] Verify every applicant and manager endpoint enforces the correct authorization.
- [?] Verify unauthorized access does not disclose another user's application data.

## 3. Properties and units

- [x] Property and unit domain models exist.
- [x] Archived properties and units are rejected when creating new applications.
- [?] Property and unit CRUD workflows.
- [ ] All managers can view all properties and units; only assigned managers can edit them.
- [?] Unit type lookup and inactive-type handling.
- [?] Archive behavior preserves historical applications and leases.
- [?] Archived units/properties cannot be used for new applications or leases.
- [?] Verify property/unit validation and database constraints.

## 4. Rental application lifecycle

- [x] Draft application creation.
- [x] Applicant information snapshot stored with the application.
- [x] Applicant information persistence.
- [x] Applicant information completion tracking.
- [x] Residence history completion tracking.
- [x] Application ownership checks in service operations.
- [x] Status transitions modeled in the domain.
- [x] Submission requires completed application sections.
- [x] Submission rejects applications in invalid statuses.
- [x] Submission checks whether the unit has a currently active lease.
- [?] Verify duplicate draft/application behavior against the original requirements.
- [?] Verify archived-property/unit and other invalid-state checks at submission time.
- [?] Verify concurrent submission, withdrawal, and status changes cannot produce invalid state.

## 5. Applicant information wizard

- [x] Applicant Information section.
- [x] Residence History section.
- [x] Summary section.
- [x] View-model factory creates section-specific view models.
- [x] Continue saves valid applicant information before advancing.
- [x] Invalid applicant information stays on the same section.
- [x] Failed service operations preserve entered applicant information.
- [x] Back navigation does not save posted changes.
- [x] Submit is restricted to the Summary section.
- [x] Read-only applications open on Summary.
- [?] Verify all section-navigation edge cases and tampered POST requests.
- [?] Verify the rendered UI and validation messages manually in a browser.

## 6. Residence history

- [x] Add residence history entries.
- [x] Update residence history entries.
- [x] Remove residence history entries.
- [x] Residence history changes invalidate the completion flag.
- [x] Completion validates required entry fields.
- [x] Move-in dates cannot be in the future.
- [x] Move-out dates cannot precede move-in dates.
- [x] Zero residence entries can be marked complete.
- [x] Application ownership and editable-status checks are enforced by the service.
- [x] Residence History section extracted into a partial view.
- [x] Summary section extracted into a partial view.
- [?] Verify modal add/edit/remove behavior, including invalid POSTs.
- [?] Verify failed modal submissions preserve entered values and validation errors.
- [?] Verify modal forms are not nested inside the main navigation form.
- [?] Verify read-only applications cannot change residence history through forged requests.

## 7. Status workflow and history

Expected workflow:

- Draft → Submitted or Withdrawn.
- Submitted → Approved, Returned, Denied, or Withdrawn.
- Returned → Submitted or Withdrawn.
- Approved, Denied, and Withdrawn are terminal states.

Progress:

- [x] Status transitions enforced by the domain.
- [x] Return requires a non-whitespace comment.
- [x] Denial requires a non-whitespace comment.
- [x] Status history records previous/new status, actor, timestamp, and comment.
- [x] Approval and lease creation occur within one transaction.
- [x] Manager property ownership is checked before status changes.
- [?] Verify the complete transition matrix with tests.
- [?] Verify applicant withdrawal rules and any lease-related restrictions.
- [?] Verify invalid status values and malformed requests are rejected safely.

## 8. Lease creation and concurrency

- [x] Approval creates a lease.
- [x] Lease term is 12 months.
- [x] Agreed rent comes from the unit's monthly rent.
- [x] Approval rejects overlapping lease periods.
- [x] Approval uses a SQL Server transaction and unit-row locking.
- [x] Concurrent-approval test uses separate DbContext instances.
- [x] `ChangeStatusAsync_TwoApplicationsApprovedConcurrently_CreatesOnlyOneLease` passed 10 consecutive runs.
- [?] Verify lease date boundaries and any timezone/date assumptions.
- [?] Verify the database has appropriate constraints to complement service-level checks.
- [?] Assess concurrency behavior for operations beyond the tested two-approval scenario.

## 9. Property manager workflows

- [?] Manager dashboard and application queue.
- [ ] Managers see all applications; review actions are limited to assigned properties.
- [?] Queue filtering.
- [?] Queue sorting with a whitelist of supported sort fields.
- [?] Database-side pagination and counts.
- [?] Stable ordering when multiple applications share a sort value.
- [?] Submitted applications sorted oldest-first where required.
- [?] Page size of 20 where specified.
- [?] Application details and status-change UI.
- [?] Return/denial comments are displayed appropriately.

## 10. Seeding and startup

- [x] Idempotent startup database migration. Evidence: `DatabaseInitializer`; `InitializeAsync_RunTwice_CreatesEachRoleOnce`; manual fresh-database startup.
- [x] Seed Identity roles idempotently.
- [?] Seed lookup/reference data.
- [?] Seed demo users, roles, and profiles.
- [?] Seed properties, units, applications, status histories, residence entries, and leases.
- [?] Use Bogus or the required data-generation approach.
- [?] Repeated startup does not duplicate seed data.
- [?] Existing user passwords and user edits are not overwritten.
- [?] Existing data is not deleted merely because it is absent from the seed definition.
- [?] Seeded records satisfy all domain relationships and status invariants.

## 11. Tests and quality

- [x] SQL Server-backed service test infrastructure exists.
- [x] Service tests cover application operations and common business failures.
- [x] Residence-history completion invalidation tests pass.
- [x] Approval/lease creation test passes.
- [x] Existing-lease approval rejection test passes.
- [x] Concurrent approval test passes 10/10 runs.
- [x] Full suite: 82/82 passing (2026-10-18).
- [?] Review missing negative cases and authorization boundaries.
- [?] Verify clean build from the solution root.
- [?] Verify no unintended tracked or untracked changes remain.
- [?] Manually smoke-test the main applicant and manager workflows.

## 12. Documentation and delivery

- [?] README contains prerequisites and setup instructions.
- [?] Document SQL Server configuration and database creation/migration.
- [?] Document demo credentials without exposing production secrets.
- [?] Document how to run the application and test suite.
- [?] Verify a fresh checkout can be configured and run using only the documentation.
- [?] Final review against the original assessment prompt.
- [?] Final Git status and commit review.

---

## Current checkpoint

**Latest known commits**

- `f760948` — Assign one property manager per property
- `7ebb25b` — Add registration service with role-based profiles
- `e5cebde` — Add database initializer with role seeding
- `8965825` — Add account registration, login, and logout
- Allow an existing account to register for an additional role

**Latest verified evidence**

- Full suite: 82/82 passing.
- Fresh-database startup applies all migrations and creates both roles.
- Manual check: register, log in, and log out as each role; manager receives Access Denied on applicant pages; existing account adds the second role with its password.

**Still to confirm**

- Property and unit management (modals, unit types, archiving).
- Applicant unit browsing and residence-history modals.
- Manager application list, review modal, and status history.
- Demo data seeding with Bogus.
- README and final review against the assessment.

## Working protocol

1. Pick one coherent requirement or small group of related requirements.
2. Identify expected behavior and relevant existing code/tests.
3. Add or update a cohesive batch of tests.
4. Run the tests and confirm expected failures when appropriate.
5. Implement the smallest appropriate change.
6. Run focused tests, then the full suite when practical.
7. Update this specification with status and test evidence.
8. Review `git diff` and `git status`.
9. Commit at logical milestones; keep unrelated changes in separate commits.

**Important:** This is a working specification based on the requirements captured so far. Reconcile it with the original assessment instructions before final delivery so no explicit requirement is missed.
