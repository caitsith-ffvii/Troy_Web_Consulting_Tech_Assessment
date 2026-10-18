# Architecture Decision Register and Open Questions

## Purpose

This document summarizes architectural decisions for the property-management assessment and records implementation details that remain unresolved.

Use it as a reference when implementation decisions arise. Update it when a meaningful decision changes.

## Confirmed Decisions

### Architecture and Organization

- Use .NET 10, ASP.NET Core MVC, Razor, EF Core, SQL Server, and ASP.NET Core Identity.
- Use feature-based organization for controllers and services.
- Use conventional MVC view locations under `Views/`.
- Keep domain entities together under `Domain/Entities/`.
- Use Fluent API configuration classes as the primary EF Core mapping approach.
- Register entity configurations through `ApplyConfigurationsFromAssembly(...)`.
- Use service interfaces where they establish useful contracts and support testing.
- Keep the architecture small; avoid abstractions that do not solve a current requirement.

### Layer Responsibilities

- Controllers coordinate HTTP requests and responses.
- Services query the database, enforce business rules, and coordinate application operations.
- Controllers map returned entities into view models.
- Services do not depend on MVC page view models.
- Business rules are enforced by services, not only by UI controls.
- Expected business failures use operation-specific service results.
- Validation results support multiple field-specific errors and general errors.
- Controllers translate service errors into MVC `ModelState` errors for display.
- Unexpected programming errors reach centralized error handling and logging.
- Services receive a scoped `DbContext` through dependency injection and do not retain it across requests or operations.

### Data Access and Persistence

- Services query the database through EF Core.
- Use async database operations consistently and pass cancellation tokens where appropriate.
- Use `AsNoTracking()` for genuinely read-only queries.
- Apply filtering, sorting, counting, and pagination in SQL Server.
- Use a scoped `DbContext`.
- Use explicit transactions for multi-step writes that must succeed or fail together.
- A single `SaveChangesAsync()` already uses EF Core's transaction behavior for its database writes.
- Preserve important historical records and configure delete behavior deliberately.
- Do not automatically retry database failures without an explicit, justified retry strategy.

### Application Workflow

- An application belongs to one applicant and one specific unit.
- Applicant Information is snapshotted for the application.
- Residence History consists of individual entries associated with the application.
- Residence History is optional; zero or more entries are allowed.
- Each provided residence entry requires an address, landlord name, landlord phone, and move-in date.
- Move-out date is optional, cannot precede move-in, and move-in cannot be in the future.
- Applicant Information requires name, phone, email, and current address. Phone and email receive format validation.
- The application uses one page with three sections: Applicant Information, Residence History, and Summary.
- Each section has its own section view model and partial or view component.
- Continue validates and saves the current section before advancing.
- Back returns to the previous section without saving current unsaved changes.
- Summary displays saved data retrieved from the database.
- Submit reloads authoritative application data and verifies ownership, completeness, status, and eligibility.
- Draft and Returned applications are editable; other statuses are locked for applicants.
- A Returned application is edited in place.
- Status history is preserved when the application changes status.
- Residence-history entries can be permanently deleted while the application is Draft or Returned, subject to authorization and successful persistence.

### Application Status Transitions

Statuses:

- `Draft`
- `Submitted`
- `Returned`
- `Approved`
- `Denied`
- `Withdrawn`

Permitted transitions:

- `Draft` → `Submitted` or `Withdrawn`
- `Submitted` → `Approved`, `Returned`, `Denied`, or `Withdrawn`
- `Returned` → `Submitted` or `Withdrawn`
- `Approved`, `Denied`, and `Withdrawn` are terminal.

An applicant may withdraw before approval and lease creation. Approval and lease creation are one atomic operation. Concurrent conflicting operations, such as approval and withdrawal, must not overwrite one another silently.

The `RentalApplication` entity owns legal status transitions. Services coordinate authorization, database-dependent checks, persistence, and status history.

A rejected transition does not create a successful status-history record. Separate auditing of failed attempts is out of scope.

### Approval and Lease Creation

- A lease belongs to a specific unit.
- Historical leases are preserved.
- Lease records capture the agreed monthly rent.
- Approval and lease creation are coordinated by `ApplicationService` initially.
- Approval must atomically update the application status, create the lease, and record status history.
- The service checks for an active lease.
- A persistence-level concurrency safeguard is required to prevent conflicting active leases while allowing historical leases.
- Failed approval must not leave a partial lease or Approved status.
- Archived properties and units cannot receive new leases.
- An existing active lease remains valid if its property or unit is archived.

### Users and Authorization

- Use separate Applicant and PropertyManager domain profiles linked to Identity users.
- Identity roles distinguish Applicant and Property Manager permissions.
- Registration allows the user to choose a role and creates the corresponding profile.
- Identity account creation and domain-profile creation must succeed or fail as one logical operation.
- A single account may have both roles; the architecture must not assume the roles are mutually exclusive.
- Applicant operations resolve the applicant profile from the authenticated account rather than trusting a posted Applicant ID.
- Each property has one assigned property manager; a manager can manage multiple properties. The manager who creates a property is assigned to it.
- Any Property Manager can view all properties, units, and applications.
- Only managers assigned to a property can edit that property and its units or review its applications.
- Authorization is enforced server-side, not merely by hiding UI controls.

### Properties, Units, and Archiving

- Properties and units have an `IsArchived` flag.
- A property or unit can be archived even when it has an active lease.
- Archiving does not terminate or modify an existing lease.
- Archiving a property does not automatically archive its units.
- Archived properties and units are excluded from normal management lists and applicant browsing.
- Archived properties and units cannot be used for new applications or leases.
- Existing applications remain in their current statuses and retain their history when a property or unit is archived.
- Archiving does not automatically withdraw or otherwise modify existing applications.
- Applications associated with archived properties or units cannot be submitted or approved into a new lease.
- Historical applications and leases remain accessible to authorized users.
- Permanent deletion is allowed only when required historical records and relationships do not need to be preserved.

### Unit Types

- `UnitType` is a reusable classification; it does not determine rent, bedroom count, or availability.
- Unit types have an active/inactive state.
- Existing units retain their assigned type when that type becomes inactive.
- Inactive types remain visible on existing units.
- New units cannot use inactive types.
- Existing units cannot be reassigned to an inactive type.
- A unit already assigned to an inactive type may be edited in other ways without changing its type.
- These rules are enforced by services, not just dropdown options.

### Application Listing

- Applicants see only their own applications.
- Property managers see all applications.
- Authorization scope, status filter, and property filter are applied in the database query.
- Lists use database-side sorting and pagination.
- Default status filter: `Submitted`.
- Default sort: oldest submission first.
- Default page size: 20.
- Supported sort fields are explicitly defined rather than accepting arbitrary client-provided expressions.
- Use a deterministic secondary sort key.
- The service accepts filtering, sorting, and pagination parameters and returns a reusable paginated result.
- The result includes `Items`, `TotalCount`, `PageNumber`, `PageSize`, and `TotalPages`.
- The controller maps results into the UI view model.
- Property filter options contain all properties.

### Modals and Validation

- Invalid modal submissions return the same partial with entered values and validation errors.
- Successful modal operations close the modal and refresh the relevant list where practical.
- Validation errors are structured so field-specific messages appear beside their fields and general errors appear in a general validation area.
- Expected validation and business-rule failures are returned as service results.
- Unexpected technical errors are handled through centralized error handling and logging.
- Technical details are not exposed in user-facing error messages.

### Startup and Seed Data

- A dedicated `DatabaseInitializer` is invoked from `Program.cs`.
- Apply migrations before seeding.
- Use stable identifiers and existence checks for idempotent seeding.
- Use deterministic Bogus configuration for sample data.
- Never overwrite existing seeded records or reset existing demo-user passwords at startup.
- Initialization failures must be visible.
- Provide demo account credentials in the README for local assessment use.

### Testing

- Start with tests against public service APIs.
- Keep tests focused on individual behaviors.
- Use mocks where they isolate the behavior under test, but avoid excessive mocking.
- Use relational integration tests for persistence, transaction, and database-constraint behavior.
- Use SQL Server or SQL Server Express for SQL Server-specific guarantees.
- Use a dedicated test database with a known starting state and cleanup.
- Test successful operations, expected business failures, persistence atomicity, and relevant concurrency safeguards.

## Working Approaches

These are the current implementation directions:

- One service per major domain area: Property, Unit, and Application.
- Application approval and lease creation initially coordinated by `ApplicationService`.
- Controllers map service-returned entities into view models.
- Reusable paginated result type for list queries.
- Fluent API configurations registered through `ApplyConfigurationsFromAssembly(...)`.
- Refresh only the relevant list after successful modal operations where practical.
- Use a dedicated registration workflow that coordinates Identity account and domain-profile creation atomically.

## Open Implementation Questions

These are implementation details, not reasons to delay beginning the project:

1. **Exact supported sort fields:** Finalize the list when implementing the UI. Likely fields include submission date, applicant name, property, and status.
2. **Active-lease database safeguard:** Choose the concrete SQL Server mechanism that prevents conflicting active leases while allowing historical leases.
3. **Known persistence failures:** Decide which specific database failures can safely be translated into expected operation results and which should propagate to centralized error handling.
4. **View component placement and purpose:** Implement the required view component where it provides useful reusable UI behavior.
5. **Concurrency handling:** Implement and test how approval, withdrawal, and other conflicting operations detect and report conflicts.
6. **Exact schema constraints and delete behavior:** Finalize foreign-key rules, unique constraints, indexes, and any SQL Server-specific constraints while implementing EF Core configurations and migrations.

## Scope Principle

Implement the smallest clear design that satisfies the assessment requirements.

Do not add speculative capabilities, generalized frameworks, or extra business rules merely because they might be useful in a larger production system.

When a requirement is unclear, identify the uncertainty, choose a reasonable minimal approach when safe, and document the decision rather than silently expanding scope.