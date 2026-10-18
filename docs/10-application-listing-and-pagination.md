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

Managers can change the status filter to view other statuses.

The default page size can be adjusted after the UI is implemented and reviewed.

## Filtering

The application list supports filtering by status and property, as required by the assessment.

Filtering must be performed in SQL Server, not after loading all applications into memory.

## Sorting

Sorting uses a defined set of supported fields rather than arbitrary client-provided field names or expressions.

Potential supported fields include:

- Submission date.
- Applicant name.
- Property.
- Application status.

The final list of supported fields should be confirmed against the implemented UI and requirements.

The default order is oldest submission first, supporting a review queue where earlier submissions remain visible for processing.

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

The initial default page size is 20. Page size and page number must be validated, with sensible defaults and bounds.

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

The exact set of sort fields and the final UI behavior can be refined when the application list is implemented and viewed live.