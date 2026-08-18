using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Payments.Domain;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace MiniBanking.Infrastructure.Messaging;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMqConnection _rabbitMqConnection;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OutboxPublisher> _logger;
    private const int BatchSize = 100;

    public OutboxPublisher(
        IServiceProvider serviceProvider,
        IRabbitMqConnection rabbitMqConnection,
        RabbitMqOptions options,
        ILogger<OutboxPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _rabbitMqConnection = rabbitMqConnection;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox batch.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task PublishBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MiniBankingDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        using var channel = _rabbitMqConnection.CreateChannel();
        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(_options.QueueName, _options.ExchangeName, _options.RoutingKey);

        foreach (var message in messages)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message.Payload);
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = message.Id.ToString();
                properties.Type = message.EventType;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                if (!string.IsNullOrEmpty(message.Headers))
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(message.Headers);
                    if (headers is not null)
                        properties.Headers = headers.ToDictionary(h => h.Key, h => (object?)h.Value);
                }

                channel.BasicPublish(
                    _options.ExchangeName,
                    _options.RoutingKey,
                    properties,
                    body);

                message.MarkPublished();
                _logger.LogInformation(
                    "Published outbox message {MessageId} of type {EventType}.",
                    message.Id,
                    message.EventType);
            }
            catch (Exception ex)
            {
                message.MarkFailed(ex.Message);
                _logger.LogError(ex, "Failed to publish outbox message {MessageId}.", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
