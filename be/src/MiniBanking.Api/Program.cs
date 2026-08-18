using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Payments.Application;
using MiniBanking.SharedKernel;
using Serilog;

// Load .env file for local development (ignored by git).
// In production, environment variables are provided by the host.
LoadEnvFile();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [CorrelationId: {CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [CorrelationId: {CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    // Add services to the container.
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    // Entity Framework Core
    var connectionString = builder.Configuration.GetConnectionString("PostgreSql");
    builder.Services.AddDbContext<MiniBankingDbContext>(options =>
    {
        options.UseNpgsql(
            connectionString,
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(MiniBankingDbContext).Assembly.GetName().Name);
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
            });
    });

    // Health checks
    var rabbitUser = builder.Configuration["RABBITMQ_USER"] ?? "minibanking";
    var rabbitPassword = builder.Configuration["RABBITMQ_PASSWORD"] ?? "minibanking_secret";
    var rabbitPort = builder.Configuration["RABBITMQ_PORT"] ?? "5672";
    var rabbitConnectionString = $"amqp://{rabbitUser}:{rabbitPassword}@localhost:{rabbitPort}";

    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString!, name: "postgresql")
        .AddRedis($"localhost:{builder.Configuration["REDIS_PORT"] ?? "6379"}", name: "redis")
        .AddRabbitMQ(rabbitConnectionString, name: "rabbitmq");

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<HmacVerificationMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // Seed demo data in development
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MiniBankingDbContext>();
        await DataSeeder.SeedAsync(dbContext);
    }

    // Health endpoint
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            var response = new
            {
                Status = report.Status.ToString(),
                TotalDuration = report.TotalDuration.TotalMilliseconds,
                Checks = report.Entries.Select(e => new
                {
                    Name = e.Key,
                    Status = e.Value.Status.ToString(),
                    Duration = e.Value.Duration.TotalMilliseconds,
                    Exception = e.Value.Exception?.Message
                })
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(response);
        }
    });

    // System info endpoint
    app.MapGet("/api/v1/system/info", () =>
    {
        return ApiResponse.Ok("Thông tin hệ thống", new
        {
            Name = "Mini Banking API",
            Version = "1.0.0",
            Framework = ".NET 8",
            Environment = app.Environment.EnvironmentName
        });
    });

    // Demo wallet endpoints
    app.MapGet("/api/v1/admin/wallets/{accountNumber}/balance", async (string accountNumber, MiniBankingDbContext db) =>
    {
        var wallet = await db.WalletAccounts
            .AsNoTracking()
            .Include(w => w.Customer)
            .FirstOrDefaultAsync(w => w.AccountNumber == accountNumber);

        if (wallet is null)
            return Results.NotFound(ApiResponse.Fail("Không tìm thấy tài khoản ví"));

        var balance = await db.BalanceSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.WalletAccountId == wallet.Id);

        return Results.Ok(ApiResponse.Ok("Thông tin số dư", new
        {
            wallet.Id,
            wallet.AccountNumber,
            wallet.Currency,
            CustomerName = wallet.Customer.FullName,
            AvailableBalance = balance?.Available.Amount ?? 0,
            LedgerBalance = balance?.Ledger.Amount ?? 0
        }));
    });

    app.MapGet("/api/v1/admin/wallets/{accountNumber}/ledger", async (string accountNumber, MiniBankingDbContext db) =>
    {
        var wallet = await db.WalletAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.AccountNumber == accountNumber);

        if (wallet is null)
            return Results.NotFound(ApiResponse.Fail("Không tìm thấy tài khoản ví"));

        var entries = await db.LedgerEntries
            .AsNoTracking()
            .Where(e => e.AccountId == wallet.Id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                e.LedgerTransactionId,
                e.AccountType,
                e.Amount,
                e.Currency,
                e.IsDebit,
                e.CreatedAt
            })
            .ToListAsync();

        return Results.Ok(ApiResponse.Ok("Lịch sử sổ cái", entries));
    });

    app.MapGet("/api/v1/demo/seed-status", async (MiniBankingDbContext db) =>
    {
        return Results.Ok(ApiResponse.Ok("Trạng thái seed", new
        {
            Customers = await db.BankingCustomers.CountAsync(),
            Wallets = await db.WalletAccounts.CountAsync(),
            BalanceSnapshots = await db.BalanceSnapshots.CountAsync(),
            LedgerTransactions = await db.LedgerTransactions.CountAsync(),
            LedgerEntries = await db.LedgerEntries.CountAsync()
        }));
    });

    // Merchant payment endpoint
    app.MapPost("/api/v1/merchant/payments", async (CreatePaymentRequest request, HttpContext context, IMediator mediator) =>
    {
        var merchantId = context.Items["MerchantId"] as string;
        var idempotencyKey = context.Items["IdempotencyKey"] as string;

        if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(idempotencyKey))
            return Results.Unauthorized();

        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var command = new CreatePaymentCommand(
            merchantId,
            idempotencyKey,
            context.Request.Method,
            context.Request.Path.Value ?? "/api/v1/merchant/payments",
            body,
            request);

        try
        {
            var response = await mediator.Send(command);
            return response.Status == "Succeeded"
                ? Results.Ok(ApiResponse.Ok("Thanh toán thành công", response))
                : Results.Ok(ApiResponse.Ok("Thanh toán thất bại", response));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ApiResponse.Fail(ex.Message));
        }
    });

    // Merchant refund endpoint
    app.MapPost("/api/v1/merchant/refunds", async (CreateRefundRequest request, HttpContext context, IMediator mediator) =>
    {
        var merchantId = context.Items["MerchantId"] as string;
        var idempotencyKey = context.Items["IdempotencyKey"] as string;

        if (string.IsNullOrWhiteSpace(merchantId) || string.IsNullOrWhiteSpace(idempotencyKey))
            return Results.Unauthorized();

        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var command = new CreateRefundCommand(
            merchantId,
            idempotencyKey,
            context.Request.Method,
            context.Request.Path.Value ?? "/api/v1/merchant/refunds",
            body,
            request);

        try
        {
            var response = await mediator.Send(command);
            return response.Status == "Succeeded"
                ? Results.Ok(ApiResponse.Ok("Hoàn tiền thành công", response))
                : Results.Ok(ApiResponse.Ok("Hoàn tiền thất bại", response));
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(ApiResponse.Fail(ex.Message));
        }
    });

    // Admin settlement endpoint
    app.MapPost("/api/v1/admin/settlements", async (CreateSettlementRequest request, IMediator mediator) =>
    {
        try
        {
            var response = await mediator.Send(new CreateSettlementCommand(request));
            return Results.Ok(ApiResponse.Ok("Quyết toán thành công", response));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ApiResponse.Fail(ex.Message));
        }
    });

    app.Run();
}
catch (Microsoft.Extensions.Hosting.HostAbortedException)
{
    // Expected during EF Core design-time tooling; rethrow without fatal logging.
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static void LoadEnvFile(string fileName = ".env")
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (directory != null)
    {
        var filePath = Path.Combine(directory.FullName, fileName);
        if (File.Exists(filePath))
        {
            LoadEnvFileFromPath(filePath);
            return;
        }

        directory = directory.Parent;
    }
}

static void LoadEnvFileFromPath(string filePath)
{
    foreach (var line in File.ReadAllLines(filePath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
            continue;

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
            continue;

        var key = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim();

        if (!string.IsNullOrEmpty(key))
            Environment.SetEnvironmentVariable(key, value);
    }
}
