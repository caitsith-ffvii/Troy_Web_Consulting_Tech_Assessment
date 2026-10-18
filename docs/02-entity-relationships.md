
# Entity Relationships

## Relationship Overview

```text
ApplicationUser
  ├── zero or one Applicant profile
  └── zero or one PropertyManager profile

Applicant
  └── many RentalApplications

Property
  ├── many Units
  └── one PropertyManager

PropertyManager
  └── many Properties

Unit
  ├── many historical RentalApplications
  └── many historical Leases, but no overlapping active leases

RentalApplication
  ├── one Applicant
  ├── one Unit
  ├── one ApplicantInformationSnapshot
  ├── many ResidenceHistoryEntries
  └── many ApplicationStatusHistory records

Lease
  ├── one Unit
  └── one associated applicant
```

## ApplicationUser and Domain Profiles

An authenticated account may have an `Applicant` profile, a `PropertyManager` profile, or both.

ASP.NET Identity roles govern which categories of actions the account may perform. Domain profiles represent the user's role-specific relationships and responsibilities.

No active-role dropdown is planned. Authorization depends on the requested action. The server resolves the appropriate domain profile from the authenticated account rather than trusting a profile ID supplied by the browser.

Whether both profiles are created at registration or when a role is granted remains undecided.

## Applicant and RentalApplication

- One applicant can submit multiple applications.
- Each application targets one specific unit.
- An application is not created without a target unit under the current design.
- The application references the applicant's domain profile.
- The application snapshot preserves the information submitted at that point in time.

## Property and PropertyManager

This is a **one-to-many relationship**:

- Each property has exactly one assigned property manager.
- A property manager can manage multiple properties.
- The manager who creates a property is assigned to it.

The manager-property relationship is distinct from role authorization:

- The `PropertyManager` role permits viewing all properties, units, and applications.
- The property assignment determines which properties the manager can edit, along with their units, and whose applications the manager can review.

The server must verify property assignment on requests that edit a property, its units, or review its applications. Hiding links or buttons in the UI is not sufficient authorization.

## Property and Unit

- A property contains multiple units.
- A unit belongs to one property.
- Unit-specific attributes include unit number, bedrooms, monthly asking rent, and unit type.

## Unit, RentalApplication, and Lease

A unit may have multiple applications over time and multiple historical leases.

Applications and leases are separate concepts with separate lifecycles.

A unit is unavailable when a lease's term covers the current date. Availability is derived from lease records rather than stored as an independently editable `IsAvailable` field.

The database must protect the invariant that a unit cannot have more than one active lease, including when concurrent approval requests race. The specific SQL Server/EF Core constraint or locking strategy remains an implementation decision.

## RentalApplication and Residence History

Each application owns a collection of residence-history entries.

Each entry is stored as its own record associated with the application. A separate `ResidenceHistory` container entity is not currently planned.

## RentalApplication and Status History

Each application has:

- A current status stored on the application.
- A collection of status-history records representing successful transitions.

The current status is stored directly for convenient workflow checks and filtering. The history collection preserves the sequence of transitions.

## Decisions to Preserve

- One application targets one specific unit.
- A unit may have multiple applications and historical leases.
- Only one active lease may cover a unit at a time.
- A lease captures the agreed rent and term.
- Submitted applicant information is snapshotted.
- Each property has one assigned property manager; a manager can manage multiple properties.
- Property managers can view all properties, units, and applications; editing and reviewing require being the property's assigned manager.
