
# Rental Application Approval Workflow

## Goal

Approving a rental application must consistently perform three changes:

1. Transition the application to `Approved`.
2. Create a lease for the unit.
3. Record the successful status transition in application status history.

These changes must succeed or fail together.

## Workflow Responsibilities

### 1. Controller and Authorization

The MVC endpoint receives the approval request and ensures the authenticated user is allowed to invoke the property-manager action.

The application/service layer must also ensure the manager is assigned to the relevant property. Role authorization alone does not grant access to every property.

### 2. Application Service

The service coordinates the workflow:

1. Load the application and relevant unit/lease information.
2. Verify the manager has access to the property's unit.
3. Check that the application can be approved.
4. Check whether an active lease already exists.
5. Invoke the application's domain transition method.
6. Create the lease using the agreed rent and lease term.
7. Create the status-history record.
8. Persist the coordinated changes atomically.
9. Return an explicit result describing success or an expected business-rule rejection.

The service should not duplicate the application's status-transition rules.

### 3. Domain Entities

`RentalApplication` checks whether its current status can legally transition to `Approved`.

The unit/lease domain can express the active-lease rule, but an in-memory object reference alone is not sufficient proof that the unit is available. The decision must account for persisted lease data and concurrent requests.

### 4. Persistence

Use a database transaction so the application status, lease, and status-history record are committed together or rolled back together.

A `try/catch` can coordinate rollback and failure handling, but a `try/catch` by itself does not undo database writes.

All participating writes must use the same transaction. Independently committed service operations would undermine the atomicity requirement.

If persistence fails after entities have been changed in memory, reload affected entities when necessary rather than trying to reverse complex state changes manually.

The transaction protects persisted data; reloading restores the in-memory view.

## Expected Business-Rule Rejection

Example: the unit already has an active lease.

Expected outcome:

- Approval is rejected.
- The application remains in its prior status, typically `Submitted`.
- No new lease is created.
- No successful `Approved` status-history entry is recorded.
- The manager receives a clear explanation.

A structured result is preferred for expected business-rule rejections. Conceptually:

```text
ApprovalResult
    Succeeded: false
    FailureReason: UnitHasActiveLease
```

The controller can translate the failure reason into a user-facing message. Avoid relying on raw message-string comparisons.

A custom exception is possible, but an active lease is an expected business outcome, not necessarily an exceptional system failure.

Unexpected technical failures, such as database connectivity errors, should be logged appropriately. Internal exception details should not be exposed to the user.

## Concurrency: Two Approvals for the Same Unit

A simple availability check is not enough. Two managers could both observe that the unit is available before either transaction commits.

The workflow therefore needs both:

- An application-level availability check for clear user feedback.
- A persistence-level concurrency safeguard that prevents two active leases for the same unit.

The safeguard is a lock: inside the approval transaction, the service selects the unit row `WITH (UPDLOCK, HOLDLOCK)` before checking for overlapping leases. A second approval for the same unit waits until the first commits or rolls back, then sees its lease and is rejected.

If one request wins and the competing request loses:

- Only one active lease may remain.
- The losing transaction must not leave its application marked `Approved` without a lease.
- The losing request should return a suitable business failure or concurrency-conflict result.

A transaction is necessary for atomicity, but by itself it does not guarantee that this race is prevented under every isolation level and query pattern.

## Scope Decisions

For the current assessment:

- Keep the approval workflow focused on the required business rules.
- Do not add lease-detail lookup to the failure result unless the UI requirement calls for it.
- Do not add a separate failed-approval audit event unless required.
- Do not implement a property-assignment history model without a requirement.
- Choose a concurrency safeguard that actually enforces the one-active-lease invariant rather than implementing multiple overlapping strategies without need.

## Implementation

- **One-active-lease invariant:** the unit-row update lock described above, plus an overlapping-lease check against persisted leases.
- **Single transaction:** `ChangeStatusAsync` opens one transaction, and the status change, lease, and status-history record are saved with one `SaveChangesAsync` before commit. Any early return disposes the uncommitted transaction, rolling it back.
- **Lease term:** start and end dates are inclusive UTC calendar dates. A lease starts on the approval date and ends the day before its 12-month anniversary, so a new lease can start on the anniversary.
- **Concurrency conflicts:** `RentalApplication.RowVersion` makes a write based on stale data (for example, approving an application the applicant has just withdrawn) fail; the service returns "This application was changed by someone else. Reload and try again."
- **Result types:** status operations share `ChangeApplicationStatusResult` (success or an error message); registration, property, and unit operations have their own result types with field-specific errors.
- **Who may review:** only the property's assigned manager, only for Submitted applications, and only with the outcomes Approve, Return, or Deny. Return and Deny require a comment.
