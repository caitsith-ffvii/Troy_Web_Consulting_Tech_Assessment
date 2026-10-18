# Troy Web Consulting — Technical Assessment

## Overview

This project is a property management web application developed as part of the Troy Web Consulting technical assessment.

The application is built with ASP.NET Core MVC and Razor, Entity Framework Core, SQL Server, and ASP.NET Core Identity. It supports property and unit management, rental applications, application review workflows, and lease creation.

The implementation emphasizes clear separation of responsibilities, server-side validation, maintainable code, data integrity, and automated testing.

## Technology Stack

- **Framework:** ASP.NET Core MVC (.NET 10)
- **Language:** C#
- **UI:** Razor views, partial views, and view components
- **Authentication and authorization:** ASP.NET Core Identity
- **Database:** SQL Server / SQL Server Express
- **Data access:** Entity Framework Core, code-first migrations
- **Test framework:** xUnit
- **Test data generation:** Bogus

## Solution Structure

```text
PropertyManagement/
├── PropertyManagement.sln
├── PropertyManagement.Web/
│   ├── Controllers/
│   ├── Features/
│   ├── ViewModels/
│   ├── Views/
│   ├── Data/
│   ├── Identity/
│   └── wwwroot/
├── PropertyManagement.Domain/
│   └── Entities/
└── PropertyManagement.Tests/
    ├── Domain/
    ├── Services/
    └── Integration/
```

### Project Responsibilities

- **PropertyManagement.Web:** ASP.NET Core MVC application, controllers, services, view models, Identity integration, database access, and application startup.
- **PropertyManagement.Domain:** Domain entities and business rules independent of MVC and database infrastructure.
- **PropertyManagement.Tests:** Automated tests for domain behavior, service logic, and database-dependent behavior.

## Prerequisites

Install the following before running the application:

- .NET 10 SDK
- SQL Server or SQL Server Express
- A compatible SQL Server client, such as SQL Server Management Studio (optional)

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd PropertyManagement
```

Replace `<repository-url>` with the repository's clone URL.

### 2. Configure the database

Update the connection string in `PropertyManagement.Web/appsettings.json` or the appropriate development configuration.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=PropertyManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Adjust the server and authentication settings to match your local SQL Server installation.

### 3. Apply database migrations

The application is intended to apply pending Entity Framework Core migrations during startup.

If migrations need to be applied manually during development, run:

```bash
dotnet ef database update \
  --project PropertyManagement.Web \
  --startup-project PropertyManagement.Web
```

If the EF Core CLI tool is not installed, install it using:

```bash
dotnet tool install --global dotnet-ef
```

### 4. Run the application

From the solution directory:

```bash
dotnet restore
dotnet build
dotnet run --project PropertyManagement.Web
```

Open the local URL printed in the terminal.

### 5. Run the tests

Run the automated test suite from the solution directory:

```bash
dotnet test
```

## Application Features

The application is intended to support the following workflows:

- Applicant and property manager registration and authentication.
- Property and unit management.
- Active and inactive unit type management.
- Browsing available units.
- Multi-section rental applications with validation.
- Residence history management.
- Application submission, return, approval, denial, and withdrawal.
- Application status history and manager review comments.
- Lease creation during application approval.
- Prevention of conflicting active leases.
- Filtering, sorting, and pagination for application listings.

## Seed Data and Demo Accounts

On startup the application applies pending migrations and seeds the database. Seeding is idempotent: existing records, including demo account passwords, are never changed, and sample data is inserted only once.

Seeded data includes:

- Unit types: Apartment, Townhouse, Studio, and Loft (inactive, still shown on the unit that uses it).
- 4 properties with 16 units; each property is assigned to one of the demo property managers.
- 36 rental applications covering every status, with status history and review comments. There are more than 20 submitted applications, so the default review queue spans multiple pages.
- Active leases on three units (making them unavailable) and one expired lease.

### Demo accounts

These credentials are for local review only.

| Role | Email | Password |
|---|---|---|
| Property Manager | `manager1@demo.local` | `Demo123!` |
| Property Manager | `manager2@demo.local` | `Demo123!` |
| Applicant | `applicant1@demo.local` – `applicant5@demo.local` | `Demo123!` |

`manager1` manages Maple Court and Riverside Commons; `manager2` manages Oak Hollow and Harbor View. All managers can view every property and application, but only the assigned manager can edit a property or review its applications.

You can also register new accounts. Registering again with an existing email and that account's password adds the other role.

## Design Considerations

The implementation follows these principles:

- **Separation of responsibilities:** Controllers handle HTTP interactions, services coordinate business operations, and domain entities enforce domain-level rules.
- **Server-side enforcement:** Authorization and business rules are enforced on the server rather than relying on UI restrictions.
- **Data integrity:** Related database changes that must succeed or fail together are handled transactionally.
- **Historical records:** Application status history and lease records are retained where needed.
- **Testability:** Business behavior is tested independently where practical, with database integration tests for persistence-specific guarantees.

## Current Status

This README describes the intended project structure and functionality. Feature descriptions, setup instructions, demo credentials, and testing details will be verified and updated as implementation progresses.

## License

Created for the Troy Web Consulting technical assessment. No separate license is specified.
