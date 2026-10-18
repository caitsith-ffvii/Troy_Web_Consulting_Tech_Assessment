# Project Structure and Coding Conventions

## Purpose

This document defines the code organization and conventions for the assessment.

The structure should make responsibilities easy to find without adding unnecessary projects or abstractions.

## Project Structure

```text
Troy_Web_Consulting_Assessment.slnx
├── PropertyManagement.Domain/
│   ├── Entities/                    # Domain entities, ApplicationUser
│   └── Identity/                    # Role names
├── PropertyManagement.Web/
│   ├── Controllers/                 # MVC controllers
│   ├── Services/                    # Business operations and their result types
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   ├── DatabaseInitializer.cs   # Migrations and role seeding at startup
│   │   ├── DemoDataSeeder.cs        # Bogus demo data
│   │   └── Configurations/          # Fluent API entity configurations
│   ├── Migrations/                  # EF Core code-first migrations
│   ├── ViewComponents/              # UnitTypeSelect, StatusHistory
│   ├── ViewModels/                  # Per-feature view models
│   ├── Views/                       # Conventional Razor view locations
│   └── wwwroot/                     # Static assets, including the modal script
└── PropertyManagement.Tests/
    ├── Entities/                    # Domain unit tests
    ├── Services/                    # SQL Server-backed service tests
    ├── ViewModels/                  # View-model mapping and validation tests
    └── Infrastructure/              # Test database and scenario helpers
```

## Organization

Controllers live in `Controllers/` and services in `Services/`, following conventional ASP.NET Core MVC layout. View models are grouped by feature under `ViewModels/` (for example `Account`, `Properties`, `RentalApplications`).

An additional Lease service should be introduced only if lease operations develop enough distinct responsibilities to justify it. Application approval and lease creation are coordinated by `RentalApplicationService`.

## Controllers and Views

Use conventional Razor view locations:

- `Views/Account/`
- `Views/Properties/`, `Views/Units/`, `Views/AvailableUnits/`
- `Views/Applications/` (applicant) and `Views/ManageApplications/` (property manager)
- `Views/Shared/` for shared partials, including the modal shell and view-component views

Modal actions return a partial view when the form must be re-rendered and `204 No Content` on success; `wwwroot/js/site.js` handles both.

## Domain Entities

Keep domain entities together in `PropertyManagement.Domain/Entities/`.

Do not introduce feature-specific entity subfolders until the project size makes them useful.

Entities represent application-domain concepts and should not depend on MVC view models.

## Data Layer

Keep database access infrastructure in `Data/`.

Use Fluent API entity configuration classes in `Data/Configurations/`. Migrations live in the Web project's `Migrations/` folder.

The database initializer belongs in the data infrastructure area because it coordinates schema initialization and seeding.

## View Models

View models define the data needed by MVC views and form submissions.

They are distinct from domain entities. Do not expose entities directly as form contracts simply because their fields happen to match.

For rental applications, a parent `RentalApplicationViewModel` holds section-specific models for Applicant Information, Residence History, and Summary.

## Services

Services are concrete classes registered with dependency injection and tested against a real SQL Server database. Interfaces are not introduced until a second implementation or a mocking need justifies them.

Prefer explicit operation names and readable result types over overly generic APIs.

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
