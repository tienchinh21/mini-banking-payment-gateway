using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
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

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

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

    app.Run();
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
