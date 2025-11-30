# AppTemplate

**AppTemplate** is a personal starter repository designed to accelerate the deployment of new projects. It provides a robust, production-ready backend foundation built with modern technologies and best practices.

## 🚀 Tech Stack

- **Framework**: [.NET 10](https://dotnet.microsoft.com/)
- **Database**: [PostgreSQL](https://www.postgresql.org/)
- **Search**: [Elasticsearch](https://www.elastic.co/)
- **Cache**: [Valkey](https://valkey.io/)
- **Observability**: [Grafana](https://grafana.com/), [OpenTelemetry](https://opentelemetry.io/)
- **Background Jobs**: [Hangfire](https://www.hangfire.io/)

## 🏗 Architecture & Features

This project is a **backend-only** solution designed to be frontend-agnostic.

- **Clean Architecture**: Separation of concerns (Domain, Application, Infrastructure, API).
- **CQRS**: Command Query Responsibility Segregation with a **handmade Mediator** pattern.
- **Outbox Pattern**: Reliable messaging and eventual consistency.
- **SDK Generation**: API client generation via **Scalar**.

## 📚 Documentation

Detailed documentation for each component can be found in the `docs/` directory:

- [**Architecture**](docs/architecture.md): Deep dive into Clean Architecture, CQRS, and the Mediator pattern.
- [**Database**](docs/database.md): PostgreSQL setup and Entity Framework Core migrations.
- [**Search & Caching**](docs/search-and-cache.md): Elasticsearch integration and Valkey caching.
- [**Observability**](docs/observability.md): Monitoring with Grafana, Prometheus, Loki, and OpenTelemetry.
- [**Background Jobs**](docs/background-jobs.md): Hangfire configuration and the Outbox pattern.
- [**API & SDK**](docs/api.md): REST API details and Scalar SDK generation.

## 🛠 Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) (or Docker Compose)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Running the Application

1.  **Start Infrastructure**:
    Run the required services (PostgreSQL, Elasticsearch, Valkey, Grafana, etc.) using Docker Compose.
    ```bash
    docker-compose up -d
    ```

2.  **Apply Migrations**:
    Navigate to the API project and apply database migrations by running the application with the `migrate` argument.

    ```bash
    cd Src/Api
    dotnet run -- migrate
    ```

3.  **Run the API**:
    Start the API application.
    ```bash
    dotnet run
    ```

4.  **Access the API**:
    The API will be available at `https://localhost:5001` (or the configured port).

## 🤝 Contributing

This is a personal template, but suggestions and improvements are welcome!
