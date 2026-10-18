# Application Forms and Modals

## Purpose

This document describes the MVC form architecture for rental applications and modal-based management operations.

The implementation must use ASP.NET Core MVC with Razor views, partial views, and view components.

## Rental Application Page

The rental application uses one page with three sections:

1. Applicant Information
2. Residence History
3. Summary

Only one section is displayed at a time.

The page uses one parent `RentalApplicationViewModel`, containing the section-specific models and a value identifying the current section.

Each section has its own model and partial view or view component as appropriate. This keeps each section's fields and validation organized without creating a separate page for every step.

## View Model Responsibilities

The parent view model coordinates the page. Section-specific models define the data needed for each section.

The service should receive only the data required for the requested operation. It should not depend on the parent view model or the structure of the application page.

The Summary section is read-only and is populated from saved application data retrieved from the database.

## Continuing Between Sections

When the applicant selects Continue:

1. The server identifies the current section and requested action.
2. The current section's input is validated.
3. If validation fails, the same section is returned with the entered values and relevant validation errors.
4. If validation succeeds, the section's data is saved to the database.
5. Only after a successful save does the application advance to the next section.

Saving a section does not submit the application. The application remains a Draft until the applicant completes the required sections and submits it.

If saving fails, the applicant remains on the current section. The UI must not indicate that unsaved changes were persisted.

Successful saves of earlier sections remain saved if a later section fails. However, multiple database writes within one section-save operation should be atomic when they must succeed or fail together.

## Back Navigation

Selecting Back returns to the previous section without saving the current section's unsaved changes.

Previously saved sections remain saved.

## Summary and Submission

The Summary section displays the application's saved information. It must not trust posted read-only values as authoritative.

When the applicant selects Submit:

- The server loads the application and its saved data from the database.
- The server verifies ownership and the application's current status.
- The server verifies that the required sections are complete.
- The server checks that the unit does not have an active lease.
- The application is transitioned to Submitted only if all required checks succeed.

The browser must not be able to submit an application by changing hidden fields or manipulating read-only HTML.

## Editing Rules

Applicant Information and Residence History are editable when the application is in Draft or Returned status.

Other statuses are read-only for applicants.

These rules must be enforced by the service, not only by disabling form controls or hiding buttons.

A Returned application is edited in place. It does not create a separate copy of the entire application. Existing status-history records remain intact.

## Residence History

Residence History is a collection of individual `ResidenceHistoryEntry` records associated with the application.

Each entry has its own database identifier. Addresses, landlords, and dates are not assumed to be unique.

Applicants can add, edit, and remove residence entries while the application is editable.

Entries can be permanently deleted while the application is in Draft or Returned status. Once the application is in a locked status, residence-history changes must be rejected by the service.

If a delete operation fails, the UI must accurately reflect the persisted state. For this assessment, wait for server confirmation before treating the entry as deleted.

## Modal Forms

Modal forms are used for:

- Adding, editing, and removing properties and units.
- Adding, editing, and removing residence-history entries.
- Reviewing an application (Approve, Return, or Deny).
- Withdrawing an application.
- Adding and editing manager notes.

The modal is populated using a partial view returned from a controller action. The modal shell (`_Modal`) is rendered outside the application's Continue/Back form, so modal forms are never nested inside it.

`wwwroot/js/site.js` handles every modal the same way: a trigger's `data-modal-url` loads the partial, and the form posts with `fetch`. The controller returns the partial again when it needs re-rendering, or `204 No Content` on success.

Remove confirmations state in advance whether the record will be deleted or archived.

### Validation Failure

If submitted data is invalid:

- The controller returns the same modal partial.
- The entered values are preserved where possible.
- Validation errors are displayed in the modal.
- The modal remains open.

The form should not redirect away and require the user to reopen the modal.

### Successful Operation

After a successful operation:

- The modal closes.
- The relevant UI is refreshed.

A trigger's `data-refresh-target` names the list to reload from its `data-refresh-url` (the property list or the residence-history table). Triggers without a target, such as review and withdrawal, reload the whole page because the status, history, and available actions all change.

## Partials and View Components

Use partial views for reusable markup and form sections, including modal content.

Use view components for reusable UI that needs its own server-side logic to obtain or prepare the data it displays.

Both must be used in the application because both are explicit assessment requirements. Their use should have a clear purpose rather than introducing them into every view indiscriminately.

In this application:

- **Partials:** each application section (`_ApplicantInformationSection`, `_ResidenceHistorySection`), the Summary, and every modal form.
- **View components:**
  - `UnitTypeSelect` queries the selectable unit types, including a unit's current inactive type.
  - `StatusHistory` queries an application's transitions and resolves who made each one.
  - `ApplicationGrid` loads the property filter options for its scope and renders the grid that the JSON endpoint fills (see doc 10).

## Editable and Read-Only Sections

Each section partial renders editable inputs or read-only values based on the view model's `IsReadOnly`, which the server sets from the application's status. The Summary is composed of the same two partials in read-only mode, and it is reused on the property manager's application page.

Locked applications open on the Summary. When an application is Returned or Denied, the applicant sees the manager's latest comment at the top of the page.

## Security

All form operations must validate the authenticated user's permissions and the application's current state on the server.

Read-only presentation is not a security boundary. Posted values, hidden fields, and client-side validation cannot replace server-side checks.