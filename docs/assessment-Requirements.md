# Technical Assessment: .NET Developer

## Table of Contents

1. [Overview](#overview)
2. [Challenge](#challenge)
3. [Technical Requirements](#technical-requirements)
4. [Functional Requirements](#functional-requirements)
   - [Users](#1-users)
   - [Properties and Units](#2-properties-and-units)
   - [Rental Application](#3-rental-application)
   - [Review](#4-review)
   - [Application List](#5-application-list)
5. [Bonus (Optional)](#bonus-optional)
6. [Considerations](#considerations)
7. [Deliverables](#deliverables)

---

## Overview

- This technical assessment is intended to demonstrate your proficiency in ASP.NET Core MVC and Razor by creating a full-stack web application.
- The ideal applicant should possess a good understanding of ASP.NET Core MVC, Entity Framework Core, database design, and server-rendered front-end development using Razor views, partial views, and view components.
- You may use any UI or JavaScript libraries you desire. **Do not use a single-page application (SPA) framework.**
- All requirements in the Technical and Functional Requirements sections must be completed. Items in the Bonus section are entirely optional, and choosing not to implement them will not negatively affect your evaluation.

## Challenge

Create a full-stack web application for a property management company that accepts rental applications for its apartments.

A property manager maintains properties and their units. An applicant applies for a unit and, if approved, receives a lease.

The system has two roles:

- **Applicant**
- **Property Manager**

Ensure that permissions are correctly enforced in both controllers and the user interface.

### Applicant capabilities

- Create, complete, submit, and withdraw applications for available units.
- Correct and resubmit applications that have been returned.

### Property Manager capabilities

- Maintain properties and units.
- Review submitted applications.
- Approve applications, issuing a twelve-month lease for the unit.

## Technical Requirements

### 1. ASP.NET Core MVC and Razor

Build the web application using ASP.NET Core MVC and Razor.

- Use controllers, view models, Razor views, partial views, and view components.
- **Both partial views and view components are required.**
- Populate modals using partial views returned by controller actions.
  - When form validation fails, return the same partial view with validation messages so the modal can be re-rendered in place.
  - When the form succeeds, close the modal and refresh the affected portion of the page.
- Structure and style the application as you see fit, following best practices.

### 2. .NET 10 and Backend

- Use .NET 10.
- Use ASP.NET Identity for user and role management.
- On application startup:
  1. Create the database and apply migrations.
  2. Seed the database idempotently with:
     - Lookup data
     - Property managers
     - Applicants
     - Properties
     - Units
     - Applications in every status
  3. Use **Bogus for .NET** to generate seed data.
- Add unit tests for business logic.

### 3. SQL Server and Entity Framework Core

- Use SQL Server or SQL Server Express.
- Use Entity Framework Core with code-first database migrations.

## Functional Requirements

### 1. Users

- Users must be able to sign up, log in, and log out.
- For convenience, the sign-up process must allow users to specify whether they are an Applicant or a Property Manager.

### 2. Properties and Units

- Property managers can add, edit, and remove properties and their units through modals.
- Each unit must include:
  - Unit number
  - Number of bedrooms
  - Monthly rent
  - Unit type
- Applicants can browse available units and start an application for a unit.
- Unit Type is a lookup with Active and Inactive values.
  - An inactive unit type must continue to display on units that already use it.
  - An inactive unit type cannot be selected for any other unit.
  - This restriction must be enforced on the server.
- Approving an application creates a lease for the unit with a start date and a twelve-month term.
- A unit whose lease term includes the current date is not available.

### 3. Rental Application

#### Application sections

An application is for one unit and contains two sections and a summary:

1. **Applicant Information**
   - Name
   - Phone
   - Email
   - Current address

2. **Residence History**
   - A list of prior residences, each containing:
     - Address
     - Landlord name
     - Landlord phone
     - Move-in date
     - Move-out date

3. **Summary**
   - A read-only view of both sections.
   - A Submit action.

#### Single-page workflow

- The application must be a single page that displays one section at a time, rather than a separate page for each section.
- One view model drives the application, and each section renders through its own partial view or view component.
- One form posts to one action. The clicked button determines the action to perform.

**Continue behavior**

- Validate the current section.
- Persist the section only if it is valid.
- Advance to the next section, or to the Summary after the final section.
- If the section is invalid, re-render it with the validation errors.

**Back behavior**

- Return to the previous section without saving.

**Submit behavior**

- Submit is available only from the Summary.
- Both sections must have been saved before submission is allowed.

#### Residence history management

- Residences must be added, edited, and removed through a modal.

#### Editing and read-only behavior

- The same section partial must support both editable and read-only rendering, based on a server-side decision.
- Applicants can edit an application only while its status is **Draft** or **Returned**.
- In all other statuses, every section must be read-only.
- Controllers must reject posts for actions that are not permitted.

#### Lease availability and application submission

- At submission, reject the action with an error if the unit has an active lease.
- Repeat this check at approval.
- Other open applications for the same unit must remain unchanged.
- The approval check must prevent a second lease from being created for the same unit during an overlapping lease period.

### 4. Review

- A property manager opens a submitted application and reviews it through a modal.
- The manager selects one of the following outcomes:
  - **Approve**
  - **Return**
  - **Deny**
- A comment is required when returning or denying an application.
- Application statuses are:
  - Draft
  - Submitted
  - Returned
  - Approved
  - Denied
  - Withdrawn
- Approved, Denied, and Withdrawn are terminal statuses.
- The application page must show property managers a history of status changes and review outcomes, including:
  - Who performed the action
  - When it occurred
  - Any associated comment

### 5. Application List

- Provide an application list that can be filtered by status and property.
- Filtering must be performed in the database, not in memory.
- Applicants can see their own applications.
- Property managers can see all applications.

## Bonus (Optional)

These requirements are entirely optional.

### 1. Paging, Sorting, and Reusable Grid

- Add paging and sorting to the application list, with both performed in the database.
- Extract the list into a reusable grid view component.
- Drive the component using a JSON endpoint that returns:
  - The requested page of rows
  - The filtered total count
- Document the endpoint with OpenAPI.

### 2. Review Queue

- Allow a property manager to claim a submitted application, marking it **Under Review**, before completing the review.
- Allow the manager to release the application back to the queue.

### 3. Property Manager Notes

- Allow property managers to create, view, and edit notes.
- Notes must be visible and editable only to property managers.
- Notes must never be rendered or returned to an applicant.

### 4. Save Sections with Validation Errors

- Allow a section to be saved even when it fails validation, while displaying the errors.
- The Summary must list everything that still blocks submission.
- Keep Submit disabled while any blocking errors remain.
- Define validation rules once per section and return errors to the corresponding fields.

### 5. Multiple Applicants per Application

- Allow more than one applicant on an application.
- Any applicant associated with the application can view and edit it.
- Ownership checks must apply to all associated applicants.

#### Concurrent editing

- When two applicants edit different sections simultaneously, their saves must not interfere with each other.
- When both applicants save the same section, the second save must be rejected as stale, with a message instructing the user to reload rather than silently overwriting the first save.
- Real-time synchronization is not required.

## Considerations

- Requirements not specifically detailed in these guidelines are left to your discretion. Use sound judgment when making implementation decisions.
- Approach the exercise like a real development task. The code should be clean, robust, and production-ready.
- Follow the standards, conventions, and best practices of the frameworks you use.
- Apply thoughtful design principles. Be prepared to explain the rationale behind your design decisions and the specifics of your implementation.
- The exercise is not timed, and a fast turnaround is not rewarded. However, the assessment must be returned within **seven days of the date it was assigned**.
- If unforeseen circumstances require additional time, communicate your requested delivery date. Failure to deliver by the agreed date may result in disqualification.

## Deliverables

- Reply to the original email with a link to your GitHub repository.
- Include a `README.md` file containing any necessary setup or installation instructions beyond those standard for the technologies used in the assessment.
