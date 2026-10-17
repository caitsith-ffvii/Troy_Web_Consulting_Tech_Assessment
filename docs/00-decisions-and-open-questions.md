# Architecture Decision Register and Open Questions

## Purpose

This document summarizes important architectural decisions for the property-management assessment and records details that are not yet finalized.

Use it as a quick reference when implementation decisions arise. Update this document when a meaningful decision changes.

## Confirmed Decisions

### Architecture and Organization

- Use .NET 10, ASP.NET Core MVC, Razor, EF Core, SQL Server, and ASP.NET Core Identity.
- Use feature-based organization for controllers and services.
- Use conventional MVC view locations under `Views/`.
- Keep domain entities together under `Domain/Entities/`.
- Use Fluent API configuration classes as the primary EF Core mapping approach.
- Use service interfaces where they establish useful contracts and support testing.
- Keep the architecture small; avoid abstractions that do not solve a current requirement.

### Layer Responsibilities

- Controllers coordinate HTTP requests and responses.
- Services query the database, enforce business rules, and coordinate application operations.
- Controllers map returned entities into view models.
- Services do not depend on MVC page view models.
- Business rules are enforced by services, not only by UI controls.
- Expected business failures use operation-specific service results.
- Unexpected programming errors reach centralized error handling and logging.

### Data Access

- Services query the database through EF Core.
- Use async database operations consistently.
- Use `AsNoTracking()` for genuinely read-only queries.
- Apply filtering, sorting, counting, and pagination in SQL Server.
- Use a scoped `DbContext`.
- Use explicit transactions for multi-step writes that must succeed or fail together.
- Preserve important historical records and configure delete behavior deliberately.

### Application Workflow

- An application belongs to one applicant and one specific unit.
- Applicant Information is snapshotted for the application.
- Residence History consists of individual entries associated with the application.
- The application uses one page with three sections: Applicant Information, Residence History, and Summary.
- Continue validates and saves the current section before advancing.
- Back returns to the previous section without saving current unsaved changes.
- Summary displays saved data retrieved from the database.
- Submit reloads authoritative application data and verifies ownership, completeness, status, and unit availability.
- Draft and Returned applications are editable; other statuses are locked for applicants.
- A Returned application is edited in place.
- Status history is preserved when the application changes status.

### Approval and Lease Creation

- A lease belongs to a specific unit.
- Historical leases are preserved.
- Lease records capture the agreed monthly rent.
- Approval and lease creation are coordinated by the application service initially.
- Approval must atomically update the application status, create the lease, and record status history.
- The service checks for an active lease.
- A persistence-level concurrency safeguard is also required to prevent conflicting active leases.
- Failed approval must not leave a partial lease or Approved status.

### Users and Authorization

- Use separate Applicant and PropertyManager domain profiles linked to Identity users.
- Identity roles distinguish Applicant and Property Manager permissions.
- A single account may be allowed to have both roles; the architecture should not assume the roles are mutually exclusive.
- Applicant operations resolve the applicant profile from the authenticated account rather than trusting a posted Applicant ID.
- Property managers can access only properties assigned to them.
- Cross-property visibility is out of scope.

### Application Listing

- Default status filter: Submitted.
- Default sort: oldest submission first.
- Supported sort fields are explicitly defined.
- Use a deterministic secondary sort key.
- Default page size: 20.
- The service accepts filtering, sorting, and pagination parameters and returns a reusable paginated result.
- The controller maps results into the UI view model.
- Authorization scope is applied before records and counts are returned.
- Property filter options contain only properties assigned to the current manager.

### Startup and Seed Data

- A dedicated `DatabaseInitializer` is invoked from `Program.cs`.
- Apply migrations before seeding.
- Use stable identifiers and existence checks for idempotent seeding.
- Use deterministic Bogus configuration for sample data.
- Never overwrite existing seeded records or reset existing demo-user passwords at startup.
- Initialization failures must be visible.

### Testing

- Start with tests against public service APIs.
- Keep tests focused on individual behaviors.
- Use mocks where they isolate the behavior under test, but avoid excessive mocking.
- Use relational integration tests for persistence, transaction, and database-constraint behavior.
- Use SQL Server or SQL Server Express for SQL Server-specific guarantees.
- Use a dedicated test database with a known starting state.

## Working Approaches

These are current implementation directions that should be checked against the full assessment requirements:

- One service per major domain area: Property, Unit, and Application.
- Application approval and lease creation initially coordinated by `ApplicationService`.
- Controllers map service-returned entities into view models.
- Reusable paginated result type for list queries.
- Fluent API configurations registered through `ApplyConfigurationsFromAssembly(...)`.
- Refresh only the relevant list after successful modal operations where practical.

## Open Questions

Do not treat the following as settled until they are explicitly decided or resolved during implementation.

1. **Exact status-transition rules:** Confirm every permitted transition against the assessment, especially resubmission from Returned and withdrawal timing.
2. **Exact supported sort fields:** The likely fields are submission date, applicant name, property, and status; finalize the list when implementing the UI.
3. **Active-lease database safeguard:** Determine the concrete SQL Server mechanism that prevents conflicting active leases while allowing historical leases.
4. **Detailed entity deletion/archive rules:** Decide entity by entity, considering business significance and historical dependencies.
5. **Validation result contract:** Determine the final structure for field-specific service validation errors.
6. **Persistence-failure handling:** Define which known database failures can be converted into safe operation results and which should propagate to centralized error handling.
7. **View component placement and purpose:** Implement the required view component where it provides useful reusable UI behavior.
8. **Concurrency handling:** Finalize how approval detects and reports a conflict when concurrent operations compete for the same unit.

## Scope Principle

The assessment should be implemented with the smallest clear design that satisfies its requirements.

Do not add speculative capabilities, generalized frameworks, or extra business rules merely because they might be useful in a larger production system.

When a requirement is unclear, identify the uncertainty, choose a reasonable minimal approach when safe, and document the decision rather than silently expanding scope.