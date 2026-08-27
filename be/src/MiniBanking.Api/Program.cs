using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Messaging;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Admin;
using MiniBanking.Modules.Payments.Application;
using MiniBanking.SharedKernel;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Security.Claims;

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

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // JWT authentication
    var jwtSecret = builder.Configuration["JWT_SECRET"] ?? "this-is-a-demo-secret-key-change-in-production";
    var jwtOptions = new JwtOptions
    {
        Secret = jwtSecret,
        Issuer = builder.Configuration["JWT_ISSUER"] ?? "MiniBanking",
        Audience = builder.Configuration["JWT_AUDIENCE"] ?? "MiniBanking",
        ExpirationHours = int.Parse(builder.Configuration["JWT_EXPIRATION_HOURS"] ?? "8")
    };
    builder.Services.AddSingleton(jwtOptions);
    builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                RoleClaimType = ClaimTypes.Role
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
        options.AddPolicy("MerchantOrAdmin", policy => policy.RequireRole("Merchant", "Admin"));
    });

    // OpenTelemetry
    var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "MiniBanking.Api";
    var serviceVersion = "1.0.0";

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName, serviceVersion: serviceVersion))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter();
        })
        .WithMetrics(metrics =>
        {
            metrics
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName, serviceVersion: serviceVersion))
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddConsoleExporter();
        });

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

    // Messaging
    var rabbitMqOptions = new RabbitMqOptions
    {
        HostName = builder.Configuration["RABBITMQ_HOSTNAME"] ?? "localhost",
        Port = int.Parse(builder.Configuration["RABBITMQ_PORT"] ?? "5672"),
        UserName = rabbitUser,
        Password = rabbitPassword
    };

    builder.Services.AddSingleton(rabbitMqOptions);
    builder.Services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
    builder.Services.AddHttpClient();
    builder.Services.AddHostedService<OutboxPublisher>();
    builder.Services.AddHostedService<WebhookConsumer>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseCors("AllowAll");
    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<HmacVerificationMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<AuditLogMiddleware>();

    // Seed demo data in development
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MiniBankingDbContext>();
        await DataSeeder.SeedAsync(dbContext);
    }

    // Health endpoint
    var healthOptions = new HealthCheckOptions
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
    };
    app.MapHealthChecks("/health", healthOptions);
    app.MapHealthChecks("/api/v1/health", healthOptions);

    // System info endpoint
    app.MapGet("/api/v1/system/info", () =>
    {
        return Results.Ok(ApiResponse.Ok("Thông tin hệ thống", new
        {
            Name = "Mini Banking API",
            Version = "1.0.0",
            Framework = ".NET 8",
            Environment = app.Environment.EnvironmentName
        }));
    });

    // Register all Admin API endpoints
    app.MapAdminEndpoints();

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

    // Demo webhook receiver for local testing
    app.MapPost("/api/v1/demo/webhook-receiver", async (HttpContext context) =>
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        Log.Information("Received webhook event: {Body}", body);
        return Results.Ok();
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
