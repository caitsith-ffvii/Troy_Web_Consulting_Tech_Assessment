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
- [x] Dependency injection, entity configuration, and project boundaries reviewed; docs 05/07/12 describe the built layout.
- [x] Startup creates and migrates a fresh database and seeds it (manual fresh-database startup; README documents it).

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
- [x] Verify role and profile provisioning for seeded users. Evidence: manual login as `applicant1@demo.local`.
- [x] Every controller requires its role (`Applicant` or `PropertyManager`); services enforce ownership and property assignment. Reviewed endpoint by endpoint.
- [x] Another applicant's application returns 404, not 403. Evidence: `FindApplicantApplicationAsync_ApplicationBelongsToAnotherApplicant_ReturnsNull`.

## 3. Properties and units

- [x] Property and unit domain models exist.
- [x] Archived properties and units are rejected when creating new applications.
- [x] Property and unit add, edit, and remove through modals; invalid submissions re-render the partial in place, and success closes the modal and refreshes the list. Evidence: `PropertyServiceTests`, `UnitServiceTests`; manual check.
- [x] All managers can view all properties and units; only the assigned manager can edit them. Evidence: unassigned-manager tests; manual check.
- [x] Unit type lookup and inactive-type handling: inactive types stay on existing units but cannot be newly selected (enforced by `UnitService`). Evidence: `UnitServiceTests`.
- [x] Remove deletes records with no applications or leases and archives the rest, preserving history. Evidence: `RemoveAsync` tests.
- [x] Archived units/properties cannot be used for new applications or leases. Evidence: `UnitOrPropertyArchived` tests.
- [x] Property/unit validation in services, plus a unique unit number per property in the database.
- [x] View component: `UnitTypeSelect` renders selectable unit types.

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
- [x] Applying again for a unit with an open (Draft, Submitted, Returned) application resumes it instead of creating a duplicate. Evidence: `CreateDraftAsync_OpenApplicationForUnitExists_ReturnsExistingApplication`.
- [x] Drafts cannot be created for units with an active lease. Evidence: `CreateDraftAsync_UnitHasActiveLease_RejectsWithoutCreatingDraft`.
- [x] Applicants browse available units (not archived, no lease covering today; filtered in SQL) and start an application. Evidence: `GetAvailableUnitsAsync` tests; manual check.
- [x] Submission rejects archived units and properties.
- [x] Concurrent submission, withdrawal, and status changes cannot silently overwrite each other (`RentalApplication.RowVersion`). Evidence: `ChangeStatusAsync_ApplicationWithdrawnAfterManagerLoadedIt_RejectsApprovalWithoutLease`.

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
- [x] Navigation validates the posted section, reads editability from the database, and allows Submit only from the Summary; services re-check every rule.
- [x] UI and validation messages checked manually in each step.
- [x] Section partials render editable or read-only from a server decision, and the Summary is composed of them in read-only mode. Evidence: `AsReadOnly_ReturnsReadOnlyCopyWithoutChangingOriginal`.
- [?] Manual check of read-only section rendering on the Summary and manager details page.

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
- [x] Residence add/edit/remove through modals, including invalid POSTs. Evidence: manual check.
- [x] Failed modal submissions re-render the partial with entered values and field errors (dates validated by `ResidenceHistoryEntryViewModel`). Evidence: `ResidenceHistoryEntryViewModelTests`; manual check.
- [x] Modal forms are not nested inside the main navigation form (`_Modal` is rendered outside it).
- [x] Read-only applications cannot change residence history through forged requests: controller actions require an owned Draft/Returned application, and the service rejects locked statuses. Evidence: `*_SubmittedApplication_Rejects*` service tests.

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
- [x] Complete transition matrix. Evidence: `CanTransitionTo_FullMatrix_AllowsOnlyDocumentedTransitions`.
- [x] Applicants can withdraw Draft, Submitted, and Returned applications; terminal statuses are rejected. Evidence: `WithdrawAsync` tests.
- [x] Managers are limited to Approve, Return, and Deny. Evidence: `ChangeStatusAsync_StatusIsNotReviewOutcome_RejectsWithoutChanges`.
- [x] Invalid status, section, and sort values are rejected or fall back to defaults; managers cannot set non-review statuses.

## 8. Lease creation and concurrency

- [x] Approval creates a lease.
- [x] Lease term is 12 months.
- [x] Agreed rent comes from the unit's monthly rent.
- [x] Approval rejects overlapping lease periods.
- [x] Approval uses a SQL Server transaction and unit-row locking.
- [x] Concurrent-approval test uses separate DbContext instances.
- [x] `ChangeStatusAsync_TwoApplicationsApprovedConcurrently_CreatesOnlyOneLease` passed 10 consecutive runs.
- [x] Lease dates are inclusive UTC calendar dates; a 12-month lease ends the day before its anniversary. Evidence: `ChangeStatusAsync_ExistingLeaseEndBoundary_ApprovesOnlyAfterLeaseEnds`.
- [x] Database constraints: foreign keys with restricted deletes, unique unit number per property, check constraints on lease dates and rent, unit rent, and bedrooms.
- [x] Optimistic concurrency protects every application write beyond the two-approval scenario.

## 9. Property manager workflows

- [x] Manager application queue (`ManageApplications`) and applicant "My applications" list, sharing `_ApplicationList`.
- [x] Managers see all applications; applicants see only their own. Evidence: `ApplicationListServiceTests`.
- [x] Review actions are limited to the property's assigned manager in the UI and service. Evidence: manual check; `ChangeStatusAsync_ManagerDoesNotOwnProperty_RejectsOperation`.
- [x] Filtering by status and property in the database. Evidence: `GetManagerApplicationsAsync_FiltersByStatusAndProperty`.
- [x] Sorting by a whitelist (submitted, applicant, property, status) in either direction.
- [x] Database-side pagination and counts. Evidence: `GetManagerApplicationsAsync_SecondPage_ReturnsThatPageAndTotals`.
- [x] Stable ordering with Id as the tie-breaker. Evidence: `GetManagerApplicationsAsync_DefaultSort_OldestSubmissionFirstWithIdTieBreaker`.
- [x] Manager default: Submitted, oldest submission first (`SubmittedAtUtc`, reset on resubmission).
- [x] Page size of 20 by default.
- [x] Manager application details (read-only Summary partial) and review modal: Approve/Return/Deny with comment required for Return/Deny. Evidence: `ReviewFormViewModelTests`, `FindManagerApplicationAsync` tests; manual check.
- [x] Applicants withdraw open applications through a confirmation modal. Evidence: manual check; `WithdrawAsync` tests.
- [x] Applicants see the latest return or denial comment as a banner, and Returned shows "action needed" in their list. Evidence: `Create_ReturnedOrDenied_MapsLatestReviewComment`; manual check.
- [x] Managers see the full status history with actor, time, and comment (`StatusHistory` view component). Evidence: `GetStatusHistoryAsync_ReturnsTransitionsInOrderWithActorEmails`.

## 10. Seeding and startup

- [x] Idempotent startup database migration. Evidence: `DatabaseInitializer`; `InitializeAsync_RunTwice_CreatesEachRoleOnce`; manual fresh-database startup.
- [x] Seed Identity roles idempotently.
- [x] Seed lookup/reference data (unit types, including one inactive type in use).
- [x] Seed demo users, roles, and profiles (through `RegistrationService`).
- [x] Seed properties, units, applications, status histories, residence entries, and leases (`DemoDataSeeder`).
- [x] Use Bogus with a fixed seed.
- [x] Repeated startup does not duplicate seed data. Evidence: `InitializeAsync_RunTwice_SeedsDemoDataOnce`.
- [x] Existing user passwords and user edits are not overwritten (existing users are skipped; sample data is inserted once, all-or-nothing).
- [x] Existing data is not deleted merely because it is absent from the seed definition (the seeder never deletes).
- [x] Seeded records satisfy all domain relationships and status invariants. Evidence: `InitializeAsync_SeededData_SatisfiesDomainInvariants`, `InitializeAsync_SeedsApplicationsInEveryStatus`.

## 11. Tests and quality

- [x] SQL Server-backed service test infrastructure exists.
- [x] Service tests cover application operations and common business failures.
- [x] Residence-history completion invalidation tests pass.
- [x] Approval/lease creation test passes.
- [x] Existing-lease approval rejection test passes.
- [x] Concurrent approval test passes 10/10 runs.
- [x] Full suite: 149/149 passing (2026-10-18).
- [x] Negative cases and authorization boundaries reviewed; unauthorized, wrong-status, archived, and concurrent cases are covered by tests.
- [x] Clean build from the solution root with no warnings.
- [?] Verify no unintended tracked or untracked changes remain.
- [x] Main applicant and manager workflows smoke-tested manually.

## 12. Documentation and delivery

- [x] README contains prerequisites and setup instructions.
- [x] SQL Server configuration and automatic database creation/migration documented.
- [x] Document demo credentials without exposing production secrets.
- [x] Running the application and test suite documented.
- [?] Verify a fresh checkout can be configured and run using only the documentation.
- [x] Final review against the assessment; the read-only section partial gap was found and fixed.
- [?] Final Git status and commit review.

---

## Current checkpoint

**Latest known commits**

- `f760948` — Assign one property manager per property
- `7ebb25b` — Add registration service with role-based profiles
- `e5cebde` — Add database initializer with role seeding
- `8965825` — Add account registration, login, and logout
- `a800ad7` — Allow an existing account to register for an additional role
- `2290455` — Reject archived units and properties at submission and approval
- `2d4650c` — Add applicant withdrawal and application concurrency checks
- `88f4ce3` — Seed demo users and sample data with Bogus
- `5661c95` — Add property and unit management through modals
- `dedc52d` — Add applicant unit browsing and resume open applications
- `72a6e16` — Add residence history modals
- `31d115a` — Add application lists and show review comments to applicants
- `c81419b` — Add manager review, status history, and applicant withdrawal
- Harden leases and constraints, reuse section partials, and update docs

**Latest verified evidence**

- Full suite: 149/149 passing; clean build with no warnings.
- Manual smoke test of the full workflow: review as assigned vs. unassigned manager, return with comment, applicant resubmits, withdraw, approve removes the unit from available units.
- Manual check: manager queue paging/sorting/filtering, applicant list, return/denial banners.
- Manual check: residence modals add/edit/remove, date validation in place, list refresh without reload, read-only applications show no residence actions.
- Manual check: available units list, Apply opens a new draft, applying again resumes it, readable step label, link back to available units.
- Manual check of property and unit modals: validation re-render, list refresh, delete vs. archive, inactive unit type handling, and assigned-manager-only editing.
- Demo seeding verified by tests and a manual login as `applicant1@demo.local`.
- Fresh-database startup applies all migrations and creates both roles.
- Manual check: register, log in, and log out as each role; manager receives Access Denied on applicant pages; existing account adds the second role with its password.

**Still to confirm**

- Manual check of read-only section rendering (Summary and manager details page).
- Fresh checkout configured and run using only the README.
- Resolve the commit on `origin/main` that is not in the local branch, then push.

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
