
# Rental Application Lifecycle

## Statuses

The currently identified statuses are:

- `Draft`
- `Submitted`
- `Returned`
- `Approved`
- `Denied`
- `Withdrawn`

`Approved`, `Denied`, and `Withdrawn` are terminal states under the current design.

## Proposed Transition Model

```text
Draft ───────────────> Submitted
  │                        │
  └──> Withdrawn           ├──> Approved
                           ├──> Returned
                           └──> Denied

Returned ────────────> Submitted
   └─────────────────> Withdrawn
```

This is the current working model. Before implementation, confirm every transition against the assessment requirements, including whether withdrawal is permitted from any additional states.

## Where Transition Rules Live

`RentalApplication` owns its current status and exposes domain methods such as:

- `Submit()`
- `Return()`
- `Approve()`
- `Deny()`
- `Withdraw()`

Each method checks whether the requested transition is legal from the current state.

This prevents different callers from implementing inconsistent transition rules.

The entity does not:

- Authorize the current user.
- Query the database for a unit's active lease.
- Create or persist a lease.
- Save status history.
- Handle MVC requests or database transactions.

The application/service layer coordinates those responsibilities.

## Applicant Submission Experience

The application form uses one page and one view model, with one section shown at a time:

1. Applicant Information
2. Residence History
3. Summary

Each section has its own partial or view component, as required by the assessment. The Summary section is read-only and provides a final review before submission.

### Continue

- Validate the currently displayed section.
- If validation fails, redisplay the same section with validation errors.
- If validation succeeds, persist the valid section data and move to the next section.

### Back

- Return to the previous section.
- Do not save the current section, according to the stated assessment behavior.

Ensure the UI and server behavior consistently follow this rule.

### Submit

- Submission is available only from the Summary section.
- The two editable sections must already have been successfully saved.
- The server must validate the request and confirm that the application is eligible for submission.
- The server must not rely on the UI alone to enforce these conditions.

## Editing and Read-Only Behavior

The same section UI may render in editable or read-only mode based on the application's state and the current user's authorization.

- Editing is allowed only while the application is `Draft` or `Returned`.
- Other states are read-only.
- The server rejects unauthorized or invalid modification requests even if a user manually submits a crafted request.

Residence-history entries can be added, edited, or removed through the required modal workflow. The server validates changes and enforces the application's editability rules.

## Status History

Every successful status transition creates an `ApplicationStatusHistory` record containing:

- Previous status
- New status
- User who made the transition
- Timestamp
- Optional comment

The application also stores its current status directly. Status history does not replace the current status field.

An attempted but rejected transition does not create a successful transition record. Logging failed attempts is a separate auditing decision and is not currently in scope.

## Application List Behavior

- Applicants see their own applications.
- Property managers see applications they are authorized to access.
- Status and property filters are applied in the database query rather than by loading all records and filtering in memory.
