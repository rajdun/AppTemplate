# Background Jobs & Outbox

## Hangfire

**Hangfire** is used for managing and processing background jobs.

- **Fire-and-forget**: Offload tasks to be processed in the background.
- **Delayed**: Schedule tasks to run at a specific time.
- **Recurring**: Run tasks on a schedule (CRON).

## Outbox Pattern

The **Outbox Pattern** is implemented to ensure data consistency between the database and external systems (like message brokers or search indexes).

1. When a state change occurs, an "Outbox Message" is saved to the database in the same transaction.
2. A background worker (powered by Hangfire or a hosted service) picks up these messages and processes them (e.g., publishing events, updating Meilisearch).
3. This guarantees at-least-once delivery.
