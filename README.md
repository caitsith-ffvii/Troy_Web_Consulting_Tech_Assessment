# Troy Web Consulting — Technical Assessment

A property management web application: property managers maintain properties and units and review rental applications; applicants browse available units, complete and submit applications, and receive a twelve-month lease when approved.

Built with ASP.NET Core MVC and Razor (.NET 10), Entity Framework Core, SQL Server, and ASP.NET Core Identity. No SPA framework is used; modals are populated with partial views returned by controller actions.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server or SQL Server Express, reachable with Windows authentication (or adjust the connection strings below)

## Getting started

### 1. Clone

```bash
git clone https://github.com/caitsith-ffvii/Troy_Web_Consulting_Tech_Assessment.git
cd Troy_Web_Consulting_Tech_Assessment
```

### 2. Configure the database connections

The application and the tests use separate databases on the same server:

| File | Database |
|---|---|
| `PropertyManagement.Web/appsettings.json` | `TroyPropertyManagement` |
| `PropertyManagement.Tests/appsettings.Test.json` | `TroyPropertyManagement_Test` |

Both default to a local default instance with Windows authentication:

```text
Server=localhost;Database=...;Trusted_Connection=True;TrustServerCertificate=True;
```

For SQL Server Express use `Server=localhost\SQLEXPRESS`; for LocalDB use `Server=(localdb)\MSSQLLocalDB`. Neither database needs to exist beforehand.

### 3. Run the application

```bash
dotnet run --project PropertyManagement.Web
```

On startup the application creates the database if needed, applies all EF Core migrations, and seeds lookup data, demo accounts, and sample data (see below). If migration or seeding fails, startup fails with the error rather than running against an incomplete database. No manual `dotnet ef` step is required.

Open the URL printed in the terminal.

### 4. Run the tests

```bash
dotnet test Troy_Web_Consulting_Assessment.slnx
```

The tests run against the real SQL Server test database (created and migrated automatically on first run) so that transactions, locking, and constraints are exercised as in production. Each test creates its own uniquely named data, so the database never needs resetting.

## Demo accounts and seed data

Seeding is idempotent: existing records, including demo account passwords, are never changed, and sample data is inserted only once.

These credentials are for local review only.

| Role | Email | Password |
|---|---|---|
| Property Manager | `manager1@demo.local` | `Demo123!` |
| Property Manager | `manager2@demo.local` | `Demo123!` |
| Applicant | `applicant1@demo.local` – `applicant5@demo.local` | `Demo123!` |

`manager1` manages Maple Court and Riverside Commons; `manager2` manages Oak Hollow and Harbor View. Every manager can view all properties and applications, but only the assigned manager can edit a property or review its applications.

Seeded data, generated with Bogus using a fixed seed:

- Unit types: Apartment, Townhouse, Studio, and Loft (inactive, still shown on the unit that uses it).
- 4 properties with 16 units.
- 36 rental applications covering every status, with status history and review comments. More than 20 are Submitted, so the default review queue spans two pages.
- Active leases on three units (so they are unavailable) and one expired lease.

You can also register new accounts and choose a role. Registering again with an existing email and that account's password adds the other role.

To start over with fresh demo data, drop the `TroyPropertyManagement` database and restart the application.

## Features

**Applicants**

- Browse available units (not archived and without a lease covering today) and start an application; applying again for the same unit resumes the open application.
- Complete the application on a single page, one section at a time: Applicant Information, Residence History (added, edited, and removed through modals), and a read-only Summary. Continue validates and saves the current section; Back returns without saving; Submit is available only from the Summary once both sections are saved.
- See the manager's comment when an application is returned or denied, correct and resubmit returned applications, and withdraw open applications.
- "My applications" list with filtering, sorting, and paging.

**Application grid and API**

- Both application lists are one reusable `ApplicationGrid` view component whose rows come from a JSON endpoint (`GET /api/applications` for managers, `GET /api/applications/mine` for applicants) returning the page of rows and the filtered total. Filtering, sorting, and paging run in the database.
- API documentation: OpenAPI document at `/openapi/v1.json` and Swagger UI at `/swagger` (Development). Log in through the app first; the API uses the same sign-in.

**Property managers**

- Add, edit, and remove properties and units through modals. Removing deletes a record that nothing references and archives it otherwise, preserving its history. Inactive unit types stay on existing units but cannot be newly selected (enforced on the server).
- Review queue of all applications: filtered by status and property, sorted, and paginated in the database. Defaults to Submitted, oldest submission first, 20 per page.
- Application page with the full status history (who, when, and comment) and, for the assigned manager, a review modal to approve, return, or deny. Return and deny require a comment. Approval creates a twelve-month lease at the unit's rent.

## Design highlights

- **Layering.** Controllers handle HTTP and map entities to view models; services enforce business rules and authorization and return explicit result types for expected failures; `RentalApplication` owns its legal status transitions.
- **Server-side enforcement.** Ownership, role, property assignment, editable status, and unit-type rules are checked by services against database state, never trusted from the browser or implied by hidden UI.
- **Lease integrity.** Approval updates the application, creates the lease, and records history in one transaction, holding an update lock on the unit row so two concurrent approvals cannot create overlapping leases.
- **Concurrency.** A row version on applications turns conflicting writes (for example, an applicant withdrawing while a manager approves) into a "changed by someone else; reload" message instead of a silent overwrite.
- **Partials and view components.** Each application section and every modal is a partial view; `UnitTypeSelect` and `StatusHistory` are view components with their own data access.

Design decisions are recorded in [`docs/`](docs/), starting with [`00-decisions-and-open-questions.md`](docs/00-decisions-and-open-questions.md).

## Solution structure

```text
PropertyManagement.Domain/   Domain entities and role names
PropertyManagement.Web/      MVC app: controllers, services, data access, migrations, views
PropertyManagement.Tests/    xUnit tests: domain, view models, and SQL Server-backed services
```

See [`docs/12-project-structure-and-conventions.md`](docs/12-project-structure-and-conventions.md) for the detailed layout.
