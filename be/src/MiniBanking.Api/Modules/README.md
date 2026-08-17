# Modules

This folder contains the business modules of the Mini Banking system.
Each module is organized as a vertical slice with its own Domain, Application,
Infrastructure, and Endpoints layers. All modules live in the same ASP.NET Core
project (modular monolith) in v1.

## Module boundaries

| Module | Responsibility |
| --- | --- |
| `Accounts` | Banking customers, wallet accounts, balance queries. |
| `Ledger` | Double-entry ledger transactions and entries, balance invariant. |
| `Payments` | Payment, refund, and settlement orchestration. |
| `Merchants` | Merchant credentials, HMAC verification, API access rules. |
| `Webhooks` | Webhook subscriptions, delivery, retry attempts. |
| `Audit` | Audit events, security events, operational history. |

## Layer conventions

```text
ModuleName/
  Domain/        -- Entities, value objects, domain events, domain services
  Application/   -- Commands, queries, handlers, DTOs, validation
  Infrastructure/-- EF repositories, external clients, module-specific persistence
  Endpoints/     -- HTTP routes/maps for this module
```

## Important rules

- Payments does not update wallet balance directly. It goes through the Ledger contract.
- Webhook delivery must not happen inside the payment database transaction.
- Audit/security events must be recorded even when a request is rejected.
