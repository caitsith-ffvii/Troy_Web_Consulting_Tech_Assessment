# Startup, Migrations, and Seed Data

## Purpose

This document describes database initialization and deterministic sample-data seeding for development and assessment review.

## Initialization Flow

A dedicated `DatabaseInitializer` is invoked during application startup from `Program.cs`.

The initializer:

1. Creates the required service scope and resolves the database context.
2. Applies pending EF Core migrations.
3. Seeds required lookup data.
4. Seeds demo users and their domain profiles.
5. Seeds sample properties, units, applications, and related data.

The order matters: migrations must run before seeding records that depend on the schema.

If migration or seeding fails, initialization should fail visibly rather than silently continuing with an unusable or partially initialized application.

## Migrations

Use EF Core code-first migrations.

Migrations live in the Web project's `Migrations/` folder. A separate migration project is unnecessary for this assessment.

## Idempotent Seeding

Initialization may run more than once without creating duplicate seed records or overwriting existing user changes.

The chosen approach combines stable identifiers with existence checks.

### Fixed Lookup Data

Use stable identifiers for fixed lookup values, including Unit Type records.

Check whether each record exists before inserting it.

### Sample Data

Use deterministic Bogus configuration for reproducible sample data. Give seeded records stable identifiers or stable seed keys so that existing records can be identified reliably.

Check for existing records before inserting new ones.

### Existing Data Must Be Preserved

Seeding must not update existing records merely because the seed definition has changed.

In particular:

- Do not overwrite existing demo-user profile data.
- Do not reset existing demo-account passwords on startup.
- Do not replace application statuses or user edits with seed defaults.
- Do not delete records simply because they are no longer part of the current seed definition.

Database uniqueness constraints provide additional protection against duplicates.

## Demo Accounts

Provide demo accounts for both Applicant and Property Manager workflows.

README documentation should explain the local demo credentials and how to use the application for review.

These credentials are for the local assessment environment, not production. Do not treat sample credentials or seeded accounts as a production authentication strategy.

An existing account should not have its password or user data reset during initialization.

## Sample Scenarios

Seed enough related data to let a reviewer explore the required workflows, including applications in the required statuses.

Seed data should make it practical to demonstrate:

- Applicant access to their own applications.
- Property managers viewing all properties and applications, and editing and reviewing only those assigned to them.
- Unit availability and unavailable units.
- Draft, Submitted, Returned, Approved, Denied, and Withdrawn application states.
- Status history.
- Existing leases and historical lease records where needed to demonstrate availability behavior.

Seed relationships must satisfy the same foreign-key and business constraints as normal application data.

## Development and Production

The assessment requires automatic migration and seeding at startup for the review environment.

Treat that behavior as an assessment-specific deployment choice. In a production system, schema migration and demo-data seeding may need separate deployment controls.

Do not silently suppress initialization failures in either environment.