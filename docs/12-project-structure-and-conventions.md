# Project Structure and Coding Conventions

## Purpose

This document defines the initial code organization and conventions for the assessment.

The structure should make responsibilities easy to find without adding unnecessary projects or abstractions.

## Proposed Project Structure

```text
PropertyManagement/
├── Controllers/                     # Optional conventional location
├── Features/
│   ├── Properties/
│   │   ├── PropertiesController.cs
│   │   ├── IPropertyService.cs
│   │   └── PropertyService.cs
│   ├── Units/
│   │   ├── UnitsController.cs
│   │   ├── IUnitService.cs
│   │   └── UnitService.cs
│   └── Applications/
│       ├── ApplicationsController.cs
│       ├── IApplicationService.cs
│       └── ApplicationService.cs
├── Domain/
│   └── Entities/
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── DatabaseInitializer.cs
│   ├── Configurations/
│   └── Migrations/
├── ViewModels/
├── Views/
│   ├── Properties/
│   ├── Units/
│   ├── Applications/
│   └── Shared/
├── wwwroot/
└── Program.cs
```

This is an initial organizational guide, not a requirement to create every folder immediately.

## Feature Organization

Group controllers, service interfaces, and service implementations by major feature.

Examples include Properties, Units, and Applications.

This makes the code for a feature easier to locate without requiring a separate project for each feature.

An additional Lease service should be introduced only if lease operations develop enough distinct responsibilities to justify it. Application approval and lease creation can initially be coordinated by `ApplicationService`.

## Controllers and Views

Controllers may live in feature folders. ASP.NET Core MVC does not require controllers to live in a folder named `Controllers`.

Use conventional Razor view locations:

- `Views/Properties/`
- `Views/Units/`
- `Views/Applications/`
- `Views/Shared/`

This keeps view discovery straightforward. Feature-local view folders would require additional view-location configuration and are unnecessary for this assessment.

## Domain Entities

Keep domain entities together in `Domain/Entities/` initially.

Do not introduce feature-specific entity subfolders until the project size makes them useful.

Entities represent application-domain concepts and should not depend on MVC view models.

## Data Layer

Keep database access infrastructure in `Data/`.

Use Fluent API entity configuration classes in `Data/Configurations/`, with migrations in `Data/Migrations/`.

The database initializer belongs in the data infrastructure area because it coordinates schema initialization and seeding.

## View Models

View models define the data needed by MVC views and form submissions.

They are distinct from domain entities. Do not expose entities directly as form contracts simply because their fields happen to match.

For rental applications, use a parent `RentalApplicationViewModel` with section-specific models for Applicant Information, Residence History, and Summary.

## Services and Interfaces

Use feature-specific service interfaces and implementations where they establish useful contracts and support dependency injection and testing.

Examples:

- `IPropertyService` / `PropertyService`
- `IUnitService` / `UnitService`
- `IApplicationService` / `ApplicationService`

Prefer explicit operation names and readable result types over overly generic APIs.

Avoid introducing abstractions before a real requirement emerges.

## Asynchronous Operations

Use async/await consistently for database operations.

- Prefer EF Core asynchronous methods.
- Pass cancellation tokens where appropriate.
- Avoid `.Result` and `.Wait()`.
- Do not mark methods `async` if they do not await asynchronous work.
- Do not run simultaneous operations against the same `DbContext`.

## Configuration and Dependency Injection

Register services and the database context through ASP.NET Core dependency injection.

Use the scoped lifetime for services that depend on the scoped EF Core context.

Keep startup configuration in `Program.cs` readable; move substantial database initialization logic into `DatabaseInitializer`.

## Scope Discipline

Choose the simplest structure that meets the assessment requirements and preserves clear responsibilities.

Do not introduce extra projects, generic repositories, elaborate inheritance hierarchies, or additional service layers without a concrete reason.