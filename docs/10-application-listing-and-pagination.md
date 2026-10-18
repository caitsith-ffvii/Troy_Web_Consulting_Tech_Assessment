# Application Listing, Filtering, Sorting, and Pagination

## Purpose

This document defines how application lists are queried and presented to applicants and property managers.

## Authorization Scope

Application queries must apply authorization scope on the server before returning records or counts.

- Applicants can see only their own applications.
- Property managers can see all applications.
- UI filters do not grant authorization.

The service must enforce these rules in its query and operation logic. The controller may perform early authorization checks, but the service must not trust client-supplied identifiers as proof of access.

## Default View

For property managers:

- Default status filter: `Submitted`.
- Default sort: oldest submission first.
- Default page size: 20 applications.
- The property filter offers all properties.

Managers can change the status filter to view other statuses, including All.

For applicants ("My applications"): all statuses, most recently submitted first, filtered to properties they have applied to.

## Filtering

The application list supports filtering by status and property, as required by the assessment.

Filtering must be performed in SQL Server, not after loading all applications into memory.

## Sorting

Sorting uses a defined set of supported fields rather than arbitrary client-provided field names or expressions.

Supported fields, each in either direction:

- Submission date (`SubmittedAtUtc`).
- Applicant name (last name, then first name, from the application snapshot).
- Property (property name, then unit number).
- Application status.

An unrecognized sort value falls back to submission date.

The default order is oldest submission first, supporting a review queue where earlier submissions remain visible for processing. `SubmittedAtUtc` records the most recent submission, so a returned application that is resubmitted rejoins the queue at the back.

The requested sort field and direction must be validated. Add a deterministic secondary sort key, such as the application ID, so records with equal primary sort values have a stable order.

## Pagination

The service accepts pagination parameters and returns a reusable paginated result.

The result contains:

- `Items`: records for the requested page.
- `TotalCount`: number of matching records across all pages.
- `PageNumber`: current page number.
- `PageSize`: number of records per page.
- `TotalPages`: total page count calculated from the count and page size.

The service performs filtering, sorting, counting, and pagination in the database query.

The controller maps the returned entities into the appropriate view model. The view model exposes the pagination information needed by the UI without making the service depend on MVC.

The default page size is 20, bounded to 1–100. A page number beyond the last page returns the last page.

## Query Order

The logical query process is:

1. Apply authorization scope.
2. Apply requested filters.
3. Count the matching records.
4. Apply the requested supported sort order and deterministic secondary sort key.
5. Apply pagination.
6. Return the page and pagination metadata.

The count must reflect the authorized, filtered result set, not all applications in the database.

## Scope

## JSON Endpoint and Grid

Both lists are the same reusable `ApplicationGrid` view component, driven by a JSON endpoint:

- `GET /api/applications`: property managers; all applications.
- `GET /api/applications/mine`: applicants; their own applications.

Query parameters: `status` ("All" or a status name), `propertyId`, `sort`, `desc`, `page`, `pageSize`. The response contains the page of rows, `totalCount` (the filtered total), `pageNumber`, `pageSize`, `totalPages`, and the filters actually applied, so each role's defaults live only on the server. Each row carries its own `detailsUrl`.

Unauthenticated and unauthorized API requests receive 401 and 403 rather than a redirect to the login page.

The view component renders the filters and table shell; `wwwroot/js/application-grid.js` fetches every page, including the first, and mirrors the applied state into the page URL so refresh and back keep the view.

The endpoints are documented with OpenAPI at `/openapi/v1.json`, with Swagger UI at `/swagger` (Development only).