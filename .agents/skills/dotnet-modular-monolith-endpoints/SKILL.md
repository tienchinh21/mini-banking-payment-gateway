---
name: dotnet-modular-monolith-endpoints
description: "Architectural rules and endpoint routing standards for .NET Modular Monolith. Enforces module isolation, vertical slice organization, and forbids route pollution in Program.cs."
---

# .NET Modular Monolith & Minimal API Endpoints Standard

Enforces clean architectural boundaries and structured endpoint routing for .NET Web APIs.

## 1. Core Principles

- **No Route Pollution in `Program.cs`**: `Program.cs` is only a composition root for Dependency Injection, middleware pipeline, and module endpoint mapping calls. It must NEVER contain raw route definitions, route handlers, inline stream reads, or database queries.
- **Vertical Slice per Module**: Each business domain (`Accounts`, `Ledger`, `Payments`, `Merchants`, `Webhooks`, `Admin`) must live in its own directory with clear internal layers:
  ```text
  Modules/{ModuleName}/
    ├── Domain/         # Entities, Value Objects, Domain Events, Domain Services
    ├── Application/    # Commands, Queries, DTOs, Handlers, Validators
    ├── Infrastructure/ # Persistence, Repositories, External Clients
    └── Endpoints/      # Minimal API route mappings (IEndpointRouteBuilder)
  ```
- **Strict Boundary Isolation**:
  - A module MUST NOT directly query or update the database entities of another module.
  - Inter-module communication must happen strictly via:
    1. **Public Contracts / Interfaces** (e.g. `ILedgerService`, `IAccountService`).
    2. **Domain / Integration Events** published in-process or out-of-process.

## 2. Endpoint Mapping Standard

Every module must expose an extension method on `IEndpointRouteBuilder` in its `Endpoints/` directory:

```csharp
// Modules/Payments/Endpoints/PaymentEndpoints.cs
namespace MiniBanking.Modules.Payments.Endpoints;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/merchant/payments")
            .WithTags("Payments");

        group.MapPost("/", CreatePayment);
        group.MapGet("/{id:guid}", GetPaymentById);

        return routes;
    }

    private static async Task<IResult> CreatePayment(
        CreatePaymentRequest request,
        HttpContext context,
        IMediator mediator)
    {
        var merchantId = context.Items["MerchantId"] as string;
        var idempotencyKey = context.Items["IdempotencyKey"] as string;

        if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(idempotencyKey))
            return Results.Unauthorized();

        var command = new CreatePaymentCommand(merchantId, idempotencyKey, request);
        var response = await mediator.Send(command);

        return response.Status == "Succeeded"
            ? Results.Ok(ApiResponse.Ok("Payment succeeded", response))
            : Results.Ok(ApiResponse.Ok("Payment failed", response));
    }
}
```

In `Program.cs`, routes are registered with a single clean line per module:
```csharp
app.MapAdminEndpoints();
app.MapPaymentEndpoints();
app.MapRefundEndpoints();
app.MapAccountEndpoints();
app.MapLedgerEndpoints();
```

## 3. Anti-Patterns & Red Flags

| Anti-Pattern | Correct Pattern |
|---|---|
| Mapping 50+ lines of routes & reading streams directly in `Program.cs` | Move to `Modules/{Module}/Endpoints/{Module}Endpoints.cs` |
| `PaymentsModule` directly executing SQL on `Accounts` table | Call `IAccountService.LockAndDebitAsync()` or domain contract |
| Handlers doing HTTP request/response parsing | Endpoints handle HTTP; Handlers receive strongly-typed Commands/Queries |
