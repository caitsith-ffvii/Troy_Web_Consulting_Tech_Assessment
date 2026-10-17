# Validation and Error Handling

## Purpose

This document defines how validation, expected failures, and unexpected technical errors are handled.

## Validation Responsibilities

Validation exists at different layers for different reasons.

### MVC Input Validation

View models can use validation attributes and other MVC validation mechanisms to detect invalid form input and provide useful feedback.

Examples include required fields, invalid email formats, and invalid date input.

This validation supports the user experience. It does not replace business-rule enforcement.

### Service and Business Validation

Services enforce business rules authoritatively so that every caller receives the same protection.

Examples include:

- Only editable applications can be modified.
- Only applications in a valid status can be submitted or approved.
- A manager must be assigned to the property's application.
- A unit cannot receive a conflicting active lease.
- An applicant can only act on their own applications.
- Required application sections must be complete before submission.

Do not rely on client-side validation or MVC validation alone for these rules.

### Database Constraints

The database provides an additional integrity boundary through foreign keys, unique constraints, required columns, and other configured constraints.

Database constraints complement service validation; they do not replace user-friendly validation messages.

## Avoiding Duplicate Rules

MVC input validation and service business validation have different responsibilities and may overlap in some areas.

Do not blindly duplicate every rule in every layer. Keep the responsibilities clear:

- The MVC layer helps identify invalid form input and display errors.
- The service enforces business rules for the operation.
- The database enforces configured data-integrity constraints.

## Service Results

Expected operation failures should use operation-specific result types rather than exceptions or generic Boolean results alone.

For example, an approval operation could return an `ApproveApplicationResult` with a success indicator and a specific failure reason.

Possible failure reasons include:

- `ApplicationNotFound`
- `ApplicationNotSubmitted`
- `UnitHasActiveLease`
- `PropertyManagerNotAuthorized`
- `ValidationFailed`
- `PersistenceFailure`

These names are illustrative; the final result contract should match the implemented service operation.

Use explicit, readable result types. Do not introduce a generalized result inheritance hierarchy before a real need exists.

## Validation Error Details

When a service detects invalid input, it should provide enough structured information for the controller to associate errors with the relevant fields.

The controller can map those errors into `ModelState`, allowing Razor views to display field-level validation messages and a suitable summary.

For modal forms, validation failure should return the same partial view with the entered values and errors preserved.

## Expected Versus Unexpected Failures

### Expected Failures

Examples include:

- A requested record does not exist.
- The application is in the wrong status.
- The user is not authorized for the requested operation.
- A unit already has an active lease.
- Input violates a known business rule.

Return an appropriate service result and let the controller translate it into useful feedback.

### Technical Failures

Database outages and other expected infrastructure failures should be handled according to the operation's needs. Where they can be reliably identified, they may be represented by an appropriate persistence-failure result with a safe user-facing message.

For example: "The operation could not be completed. Please try again."

Do not expose raw database exceptions or sensitive technical details to users.

### Unexpected Programming Errors

Unexpected errors such as null-reference bugs or violated programming assumptions should not be disguised as ordinary business failures.

Allow them to reach centralized error handling and logging, unless a specific layer can handle them meaningfully and safely.

## Logging

Use centralized logging and error handling for unexpected failures.

Log enough technical context to investigate the issue while avoiding unnecessary exposure of personal information or other sensitive data.

Do not use exceptions as normal control flow for expected business outcomes.

## Transactions and Failure Results

A failure result does not itself roll back database changes.

For multi-step operations, use an appropriate database transaction and roll back when a required step fails.

After rollback, be mindful that tracked in-memory entities may no longer reflect the persisted database state. Reload or discard affected tracked state as appropriate.

## User-Facing Responses

The controller decides how to present a service result in the context of the HTTP request.

Examples:

- Field-validation errors are displayed beside the relevant fields.
- Business-rule failures receive clear, non-technical explanations.
- A successful modal operation closes the modal and refreshes the relevant UI.
- Unexpected errors are handled centrally and do not expose internal exception details.