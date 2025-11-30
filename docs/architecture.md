# Architecture

This project follows the principles of **Clean Architecture** and **CQRS** (Command Query Responsibility Segregation).

## Structure

The solution is divided into the following projects:

- **Domain**: Contains the core business logic, entities, and domain events. It has no dependencies on other projects.
- **Application**: Contains the application logic, including Commands, Queries, and their handlers. It depends on the Domain project.
- **Infrastructure**: Contains the implementation of interfaces defined in Application and Domain (e.g., database access, external services). It depends on Application and Domain.
- **Api**: The entry point of the application (REST API). It depends on Application and Infrastructure.
- **WorkerService**: A background worker for processing background jobs and outbox messages.

## CQRS & Mediator

The application uses the **CQRS** pattern to separate read and write operations.

- **Commands**: Modify state.
- **Queries**: Retrieve data.

A **handmade Mediator** pattern is implemented to decouple the sender of a request (API) from its handler (Application). This avoids a hard dependency on third-party libraries like MediatR, giving us full control over the dispatching pipeline.
