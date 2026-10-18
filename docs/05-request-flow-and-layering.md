# Request Flow and Layer Responsibilities

## Purpose

This document defines how requests move through the application and establishes the responsibilities of controllers, services, EF Core, and the database.

The goal is to keep the application understandable, testable, and consistent without introducing unnecessary architectural layers.

## Standard Request Flow

For a typical request that retrieves and displays data, the flow is:

1. The browser sends an HTTP request.
2. The controller receives the request and identifies the authenticated user.
3. The controller passes the necessary parameters and user context to the appropriate service.
4. The service queries the database through EF Core.
5. The service returns the requested entities or an operation-specific result.
6. The controller maps the returned entities into the appropriate view model.
7. The controller returns a view or other HTTP response.
8. The view renders the response for the browser.

For a request that changes data, the flow is similar, except the service applies the business rules, modifies the entities, and persists the changes before returning its result.

## Controllers

Controllers coordinate HTTP requests and responses.

Responsibilities:

- Receive route values, query parameters, form submissions, and other request data.
- Identify the authenticated user and pass the necessary user context to services.
- Call the appropriate service operation.
- Map returned entities into view models.
- Translate expected service results into appropriate HTTP responses, views, validation messages, or redirects.
- Return the appropriate partial view when a modal form must be re-rendered with validation errors.

Controllers should not own the core business rules or independently perform multi-step business workflows.

A controller may perform early checks or input validation for a better user experience, but the service remains responsible for enforcing business rules.

## Services

Services implement application operations and coordinate business workflows.

Responsibilities:

- Query the database through EF Core.
- Load the entities needed for an operation.
- Enforce business rules and authorization requirements relevant to the operation.
- Apply changes to entities.
- Coordinate related operations that must succeed or fail together.
- Persist changes.
- Return operation-specific results for expected failures.

Services should not depend on MVC view models or the shape of a particular page. They should accept the information needed for the operation and return data or results suitable for the controller to present.

For example, saving the Applicant Information section should receive that section's data, the application identifier, and the authenticated user's context—not the entire page view model.

## Service Interfaces

Use interfaces for services where they define a useful contract and support dependency injection and testing.

Examples:

- `IPropertyService`
- `IUnitService`
- `IApplicationService`

Create interfaces alongside their implementations rather than introducing abstractions without a clear purpose.

Do not introduce a generalized service-result hierarchy or additional coordinating services unless a real requirement justifies them.

## Database Access

Services use EF Core and the application's `DbContext` to query and persist data.

- Use asynchronous EF Core operations consistently.
- Use `AsNoTracking()` for genuinely read-only queries.
- Use tracked entities when the operation intends to modify them.
- Apply filtering, sorting, counting, and pagination in the database rather than loading all matching records into memory.
- Pass cancellation tokens to asynchronous database operations where appropriate.
- Do not run concurrent operations against the same `DbContext`.
- Do not keep a `DbContext` alive across unrelated requests or service operations.

The `DbContext` is registered with a scoped lifetime.

## Authorization

Authorization is enforced on the server, not by hiding or showing UI controls.

- Applicant operations must verify that the application belongs to the authenticated applicant.
- Any property manager can view all properties, units, and applications.
- Property manager operations that edit a property or its units, or review its applications, must verify that the manager is assigned to that property.
- Services must not blindly trust applicant IDs, manager IDs, or other authorization-sensitive values supplied by the browser.

Controllers and ASP.NET Core authorization can reject unauthorized requests early. Services must still enforce the business authorization requirements for the operations they execute.

## View Models and Mapping

MVC view models define the data needed by a page or form.

For this assessment, the controller maps service-returned entities into the appropriate view models. The service should not depend on MVC page structure.

When saving a form, the controller receives the view model and passes the relevant section data to the service. The service validates and persists the operation using authoritative database state.

Read-only displays must not rely on values posted by the browser as authoritative. When an operation depends on saved application data, the service loads that data from the database.

## Error Handling

Expected business failures should be represented by operation-specific service results.

Examples include:

- The requested application does not exist.
- The application is not in a state that permits the operation.
- The manager is not authorized for the property's application.
- The unit already has an active lease.
- The submitted data fails a business rule.

The controller translates these results into user-facing feedback or an appropriate HTTP response.

Unexpected programming errors should propagate to centralized error handling and logging. Do not catch every exception and disguise it as an ordinary business failure.

Technical details should be logged appropriately without exposing sensitive exception information to users.

## Transactions

Transactions belong in the service layer that coordinates the multi-step operation.

For example, application approval must coordinate:

- Changing the application's status.
- Creating the lease.
- Recording status history.

These changes must succeed or fail together. A database transaction should cover the related writes.

Do not wrap every service method in an explicit transaction unnecessarily. A single `SaveChangesAsync()` is already transactional for its database writes in EF Core.

## Guiding Principle

Keep responsibilities clear:

- **Controller:** coordinates the HTTP request and response.
- **Service:** executes the application operation and enforces its rules.
- **EF Core:** translates database queries and tracks/persists entity changes.
- **Database:** stores durable data and enforces configured integrity constraints.
- **View model:** defines the data needed by the UI.

Each layer should have a clear purpose without duplicating the responsibilities of the others.