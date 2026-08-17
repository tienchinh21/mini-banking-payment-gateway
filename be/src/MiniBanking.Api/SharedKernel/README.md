# SharedKernel

Contains domain primitives and cross-cutting concepts shared by multiple modules.

Examples:

- `Money` -- value object for amount + currency (minor unit `long`, default `VND`).
- `Entity` -- base class for entities with domain events.
- `DomainEvent` -- base class for domain events.
- `Result` -- lightweight result type for operation outcomes.

Keep this folder small. If a concept is only used by one module, it belongs in that module.
