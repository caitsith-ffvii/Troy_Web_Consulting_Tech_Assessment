
# Core Entities

## Purpose

This document captures the current domain-model decisions for the property management assessment. It is a working architecture document, not a complete implementation specification.

## ApplicationUser

**Responsibility:** Represent the authenticated account managed by ASP.NET Core Identity.

- Owns account identity and authentication-related information.
- Uses ASP.NET Identity roles such as `Applicant` and `PropertyManager` for authorization.
- Does not own rental-domain behavior merely because the account has a role.
- A single account may have both roles, subject to business rules and authorization policy.

## Applicant

**Responsibility:** Represent a person in the rental-application domain.

- Linked to an `ApplicationUser`.
- Associated with the applicant's rental applications.
- Separate from the Identity account so rental-domain relationships do not need to live directly on the authentication entity.
- Does not replace the application-time snapshot of submitted personal information.

## PropertyManager

**Responsibility:** Represent a person in the property-management domain.

- Linked to an `ApplicationUser`.
- Associated with the properties they manage.
- Can view all properties, units, and applications.
- Editing a property or its units, and reviewing its applications, requires being assigned to that property, not merely having the `PropertyManager` role.

## Property

**Responsibility:** Represent a rental property.

- Has identifying information such as name and address.
- Contains units.
- Has one assigned property manager.

## Unit

**Responsibility:** Represent a rentable unit within a property.

- Belongs to a property.
- Has attributes such as unit number, bedrooms, monthly asking rent, and unit type.
- Availability is derived from lease state rather than stored as an independently editable boolean that can drift out of sync.
- Availability rules must be enforced against persisted data, including under concurrent requests.

## RentalApplication

**Responsibility:** Represent an applicant's request to rent a specific unit.

- References an applicant and a unit.
- Owns its workflow status.
- Enforces legal status transitions through domain methods.
- Has an application-time snapshot of the applicant information submitted with the application.
- Has a collection of residence-history records.
- Has associated status-history records.
- Does not coordinate database persistence, authorization, or lease creation itself.

## ApplicantInformationSnapshot

**Responsibility:** Preserve the applicant information as submitted at the time of application.

Stores the relevant submitted information, such as:

- Name
- Phone
- Email
- Current address

This is separate from the applicant's current profile/account information. If the applicant later updates their profile, the historical application snapshot remains unchanged.

The precise table/entity shape and naming remain implementation decisions.

## ResidenceHistoryEntry

Each residence entry represents one residence supplied for an application.

Includes:

- Address
- Landlord name
- Landlord phone
- Move-in date
- Move-out date

Each entry is stored as a separate record associated directly with its `RentalApplication`. The current direction is for the application to own a collection of these entries; an additional container entity is not planned unless a requirement justifies it.

## Lease

**Responsibility:** Represent a lease agreement for a unit.

- References a specific unit and the applicant associated with the lease.
- References the approved application that created it, when there is one. Approved applications show the lease's agreed rent and dates, not the unit's current asking rent.
- Stores the agreed rent and lease term, including start and end dates.
- Preserves historical leases rather than overwriting them when a unit is rented again.
- Enforces the business invariant that a unit cannot have overlapping active leases.
- Captures the agreed rent on the lease rather than depending on the unit's potentially changing asking rent.

## ApplicationStatusHistory

**Responsibility:** Record successful application status transitions.

Each record includes:

- Previous status
- New status
- User who made the change
- Timestamp
- Optional comment

A failed transition does not create a successful status-transition record. Auditing failed attempts is a separate concern and is not currently in scope.

## ApplicationNote

**Responsibility:** An internal note on an application, for property managers only.

- References the application and the authoring user.
- Stores the text, the creation time, and the last-edit time.
- The relationship is one-way: `RentalApplication` has no notes collection, so notes cannot be loaded alongside an application on the applicant side.

## Application Services

Services coordinate workflows involving multiple entities or persistence operations, including:

- Application submission
- Application review
- Approval and lease creation
- Status-history creation

Services check business conditions that require database queries and return explicit results for expected business-rule rejections.

## MVC Controllers

Controllers:

- Handle HTTP requests.
- Apply or invoke authorization requirements.
- Call application services.
- Translate service outcomes into appropriate views and user-facing messages.

Controllers should not contain the entire business workflow.

## Design Principles

- Separate authentication identity from rental-domain responsibilities.
- Keep legal state transitions within the entity that owns the state.
- Use services to coordinate multi-entity workflows.
- Preserve submitted snapshots and historical leases.
- Avoid adding entities or abstractions solely to anticipate hypothetical future requirements.
