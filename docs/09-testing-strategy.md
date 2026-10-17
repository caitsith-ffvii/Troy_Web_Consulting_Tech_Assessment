# Testing Strategy

## Purpose

This document defines the testing approach for the assessment, with emphasis on business behavior, persistence integrity, and focused tests.

## Testing Principles

- Tests should help shape and verify the public service API.
- Each test should focus on one behavior or outcome.
- Test names should describe the scenario and expected result.
- Avoid excessive mocking that makes tests verify implementation details rather than behavior.
- Use the real relational database provider for behavior that depends on database transactions, constraints, or SQL Server semantics.
- Do not rely on unit tests alone to prove persistence correctness.

A focused test may contain multiple assertions when needed to establish one behavior. Atomicity means testing one coherent behavior, not necessarily limiting every test to exactly one assertion.

## Unit Tests

Unit tests should focus on business logic and service outcomes that can be isolated from the database.

Examples include:

- A Draft application can be submitted when required information is complete.
- An application in an invalid status cannot be approved.
- A Returned application can be edited.
- A Submitted application cannot be edited by the applicant.
- An unauthorized manager cannot perform a protected operation.
- An approval request fails when the service determines the unit already has an active lease.
- An invalid status transition is rejected.
- A successful transition produces the appropriate operation result.

Start with tests against the public service API. Tests should reflect observable behavior rather than requiring a particular private implementation.

Mock dependencies only where doing so helps isolate the behavior under test.

## Integration Tests

Integration tests should verify behavior that depends on EF Core, the relational database, or database constraints.

Examples include:

- Saving an application section persists the expected data.
- A section operation that requires multiple writes rolls back if a required write fails.
- Application approval creates a lease and status-history entry together.
- A failed approval leaves no partial lease or Approved status in the database.
- Database constraints prevent conflicting active leases for the same unit.
- Application-list filtering, sorting, and pagination produce the expected results.
- Authorization scope is applied before records and counts are returned.

Use SQL Server or SQL Server Express for tests that depend on SQL Server-specific behavior. A non-relational substitute or a different database provider is not sufficient to prove SQL Server transaction or constraint behavior.

## Test Database Management

Use a dedicated test database with a known starting state.

Tests must not depend on data left behind by unrelated tests or developer activity. Arrange the required records before each test or test group and clean up reliably afterward.

Keep test data deterministic so that test outcomes do not depend on random values.

## Atomicity Tests

For multi-write operations, test the externally observable result of a failure.

For example, if approval fails during lease creation or status-history persistence, verify that the database does not retain only part of the approval.

Use a relational integration test to verify the transaction boundary. A unit test with mocked repositories cannot by itself establish that the database transaction works correctly.

## Concurrency Tests

The active-lease rule must hold even when two approval operations race.

A suitable integration test should attempt conflicting operations and verify that the database ends in a valid state with no conflicting active leases.

The exact concurrency mechanism and test implementation remain to be determined during implementation.

## Test Scope

Prioritize required business workflows and failure conditions before optional enhancements.

At minimum, cover:

- Application section validation and saving.
- Application submission.
- Valid and invalid status transitions.
- Applicant ownership and manager authorization.
- Approval and lease creation.
- Active-lease conflicts.
- Transaction rollback.
- Application-list filtering, sorting, and pagination.

Do not introduce a complex testing framework or elaborate test-data architecture unless the assessment's scope requires it.