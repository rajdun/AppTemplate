# Database

The project uses **PostgreSQL** as the primary relational database.

## Entity Framework Core

Data access is handled via **Entity Framework Core**.

- **Migrations**: Database schema changes are managed through EF Core migrations.
- **Configuration**: Database connection strings are configured in `appsettings.json`.

## Setup

The database is containerized and defined in `compose.yaml`.
