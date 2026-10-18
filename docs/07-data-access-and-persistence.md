# Data Access and Persistence

## Purpose

This document establishes how the application accesses SQL Server, configures EF Core entities, and persists changes.

## Technology

- .NET 10
- ASP.NET Core MVC with Razor
- Entity Framework Core
- SQL Server or SQL Server Express
- Code-first EF Core migrations
- ASP.NET Core Identity for users and roles

Migrations live in the Web project's `Migrations/` folder. A separate migrations project is not required for this assessment.

## DbContext

The application uses an `ApplicationDbContext` that includes the Identity model and application entities.

Register the context with dependency injection using a scoped lifetime.

A context should normally serve the current request and its associated database operations. Do not retain it across unrelated requests or use the same context for simultaneous database operations.

## Entity Configuration

Use Fluent API configuration classes as the primary approach for EF Core mapping.

Example structure:

- `Data/Configurations/PropertyConfiguration.cs`
- `Data/Configurations/UnitConfiguration.cs`
- `Data/Configurations/RentalApplicationConfiguration.cs`
- `Data/Configurations/LeaseConfiguration.cs`
- `Data/Configurations/ApplicationStatusHistoryConfiguration.cs`

Each configuration implements `IEntityTypeConfiguration<T>`.

Register configurations through `ApplyConfigurationsFromAssembly(...)`.

Data annotations may be used selectively where appropriate, but database relationships, indexes, delete behavior, and other substantial mapping rules should be configured explicitly through Fluent API.

## Queries

Services are responsible for querying the database.

Use asynchronous EF Core operations consistently, including methods such as:

- `ToListAsync()`
- `FirstOrDefaultAsync()`
- `AnyAsync()`
- `CountAsync()`
- `SaveChangesAsync()`

Pass cancellation tokens where appropriate. Avoid synchronous blocking through `.Result` or `.Wait()`.

### Tracking

Use `AsNoTracking()` for queries that are genuinely read-only.

Use tracked entities when the operation intends to modify the entities and persist those modifications through the same context.

A query that initially appears to be a read may need tracking when it is part of a workflow that changes the loaded entities.

### Filtering and Pagination

Filtering, sorting, counting, and pagination should be performed in SQL Server before the matching records are returned.

Do not load an entire application list and then filter or paginate it in memory.

Apply the current user's authorization scope to the query so unauthorized records are not included in returned results or counts.

## Saving Changes

Use `SaveChangesAsync()` to persist entity modifications.

A single `SaveChangesAsync()` call is transactional for the database writes it performs when the provider supports transactions, as SQL Server does.

Use an explicit transaction when an operation contains multiple database writes that must succeed or fail together and cannot be safely handled in one `SaveChangesAsync()` call.

## Transactions

The application-approval workflow is a required multi-step transaction.

The following changes must succeed or fail together:

- Transitioning the application to Approved.
- Creating the lease.
- Adding the corresponding status-history entry.

If any required write fails, the transaction must roll back so that the database does not contain a partially completed approval.

A transaction does not automatically solve every concurrency race. The database must also enforce the rule that conflicting active leases cannot be created for the same unit.

## Relationships and Delete Behavior

Configure relationships and delete behaviors deliberately.

Historical applications, leases, and status-history records must not be accidentally deleted through cascading deletes.

Where records represent important business history, preserve them and use entity-specific archive or status behavior where appropriate.

Do not assume every entity with a foreign key must be archived in every circumstance. Evaluate deletion based on the entity's business significance and its dependent records.

Exact delete and archive rules should be documented for each entity as they are finalized.

## Database Constraints

Use database constraints to protect data integrity in addition to service-level validation.

Examples include:

- Required fields and appropriate maximum lengths.
- Foreign-key constraints.
- Unique constraints where the business rules require uniqueness.
- Appropriate indexes for common query and relationship patterns.
- A persistence-level safeguard against overlapping active leases for a unit.

The precise SQL Server implementation of the active-lease concurrency safeguard remains an implementation detail to determine during development.

## Entity-to-View-Model Mapping

Services return entities or operation-specific results rather than depending on MVC view models.

Controllers map the returned entities into the view models required by the relevant views.

For read-only pages, the service retrieves the necessary data from the database and the controller maps it for presentation.

## Migrations

Database schema changes are represented by EF Core migrations.

The database initializer applies pending migrations before inserting seed data.

If migration execution fails, application initialization must fail visibly rather than silently continuing with an incompatible database schema.