# Infrastructure

Cross-cutting infrastructure concerns used by multiple modules.

## Folders

- `Persistence/` -- `DbContext`, EF Core configuration, migrations, raw SQL helpers.
- `Messaging/` -- RabbitMQ publisher/consumer abstractions, outbox publisher worker.
- `Security/` -- HMAC verification, JWT/RBAC helpers, correlation ID middleware.
- `BackgroundJobs/` -- Hosted services such as outbox publisher and webhook delivery worker.

## Rule

Infrastructure here is shared. Module-specific persistence code (e.g., a repository
only used by the Ledger module) should live inside `Modules/Ledger/Infrastructure/`.
