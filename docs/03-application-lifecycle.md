# Rental Application Lifecycle

## Statuses

The application statuses are:

- `Draft`
- `Submitted`
- `Returned`
- `Approved`
- `Denied`
- `Withdrawn`

`Approved`, `Denied`, and `Withdrawn` are terminal states.

## Status Transitions

```text
Draft ───────────────> Submitted
  │                       │
  └──> Withdrawn          ├──> Approved
                          ├──> Returned
                          ├──> Denied
                          └──> Withdrawn

Returned ────────────> Submitted
   └─────────────────> Withdrawn
```

### Transition Rules

- `Draft` can be submitted or withdrawn.
- `Submitted` can be approved, returned, denied, or withdrawn.
- `Returned` can be resubmitted or withdrawn.
- `Approved`, `Denied`, and `Withdrawn` cannot transition to another status.
- An applicant may withdraw an application before it is approved and its lease is created.
- Approval and lease creation are one atomic operation. Once approved, the application cannot be withdrawn.
- Submission and approval must reject the operation if the property or unit is archived.
- Approval must also reject the operation if the unit already has an active lease.

The `RentalApplication` entity owns its current status and exposes domain methods such as:

- `Submit()`
- `Return()`
- `Approve()`
- `Deny()`
- `Withdraw()`

Each method enforces legal status transitions. The application/service layer coordinates database-dependent checks, authorization, lease creation, status history, and persistence.

The service must protect against concurrent operations, such as an applicant withdrawing while a property manager approves the same application. Only one conflicting operation may succeed.

## Applicant Submission Experience

The application form uses one page and one parent view model, with one section displayed at a time:

1. Applicant Information
2. Residence History
3. Summary

Each section has its own view model and partial or view component, as required by the assessment. The Summary section is read-only and displays the persisted application data.

### Continue

- Validate the currently displayed section.
- If validation fails, redisplay the same section with errors.
- If validation succeeds, persist the section data and advance to the next section.
- If persistence fails, remain on the current section and preserve the user's input.
- A section save does not submit the application or change its status.

### Back

- Return to the previous section.
- Do not save changes made in the current section.
- Previously saved sections remain persisted.

### Submit

- Submission is available only from the Summary section.
- Applicant Information and Residence History must have been successfully saved.
- The server reloads authoritative application data and validates ownership, current status, completeness, and eligibility.
- The server rejects submission if the property or unit is archived.
- The server does not trust read-only values or workflow state supplied by the browser.
- Successful submission changes the status from `Draft` or `Returned` to `Submitted` and records the transition in status history.

## Application Data and Editing

The application stores an applicant information snapshot so that the submitted application retains the name, phone, email, and current address provided for that application, even if the applicant's profile later changes.

- A `Draft` application can be edited.
- A `Returned` application can be edited and resubmitted.
- Other statuses are read-only.
- Returning an application allows the applicant to update the existing application; it does not create a separate application record.
- Status history preserves prior transitions and comments even when application data is subsequently edited.

The same section UI may render in editable or read-only mode based on the application's status and the user's authorization. The server enforces these rules regardless of the UI.

## Residence History

Residence history is an applicant-provided collection, not a representation of the application's internal lease history.

- Zero or more residence entries are allowed.
- Each entry has its own database ID.
- Each provided entry requires an address, landlord name, landlord phone, and move-in date.
- Move-out date is optional.
- Move-in date cannot be in the future.
- Move-out date cannot precede move-in date.
- Entries can be added, edited, or removed while the application is `Draft` or `Returned`.
- Entries are locked in other statuses.
- Draft and Returned entries may be permanently deleted, provided the deletion is authorized and succeeds.

Residence-history changes are validated and persisted by the service layer. Modal requests return field-specific and general errors when validation fails.

## Archived Properties and Units

Archiving removes a property or unit from new business activity without destroying its historical records.

- Archived properties and units cannot receive new applications or new leases.
- Existing applications remain in their current statuses and retain their history.
- Archiving does not automatically withdraw or otherwise modify existing applications.
- An application for an archived property or unit cannot be submitted or approved into a new lease.
- An active lease remains valid when its property or unit is archived.
- Archiving a property does not automatically archive its units.
- Archived records remain available for authorized historical review.

The service layer must enforce archived-state rules; hiding records in the UI is not sufficient.

## Status History

Every successful status transition creates an `ApplicationStatusHistory` record containing:

- Previous status
- New status
- User who made the transition
- Timestamp
- Optional comment

The application stores its current status directly. Status history does not replace the current status field.

An attempted but rejected transition does not create a successful transition record. Auditing failed attempts is outside the current scope.

## Application List Behavior

- Applicants see only their own applications.
- Property managers see applications for properties they are assigned to manage.
- The authorization scope, status filter, and property filter are applied in the database query.
- Lists use database-side sorting and pagination.
- The default manager review queue filters to `Submitted`, sorts by oldest submission first, and uses a page size of 20.
- Supported sort fields are explicit rather than arbitrary client-provided expressions.
- Archived properties and units are excluded from normal applicant browsing and normal management lists, while historical applications remain accessible to authorized users.