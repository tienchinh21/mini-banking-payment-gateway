using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Payments.Domain;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MiniBanking.Infrastructure.Messaging;

public class WebhookConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMqConnection _rabbitMqConnection;
    private readonly RabbitMqOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookConsumer> _logger;
    private IModel? _channel;

    public WebhookConsumer(
        IServiceProvider serviceProvider,
        IRabbitMqConnection rabbitMqConnection,
        RabbitMqOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _rabbitMqConnection = rabbitMqConnection;
        _options = options;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _rabbitMqConnection.CreateChannel();

        // 1. Declare Dead Letter Exchange & Queue (DLQ)
        _channel.ExchangeDeclare(_options.DeadLetterExchangeName, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(_options.DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(_options.DeadLetterQueueName, _options.DeadLetterExchangeName, _options.DeadLetterRoutingKey);

        // 2. Declare Main Exchange & Queue with DLQ arguments
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", _options.DeadLetterExchangeName },
            { "x-dead-letter-routing-key", _options.DeadLetterRoutingKey }
        };

        _channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueBind(_options.QueueName, _options.ExchangeName, _options.RoutingKey);
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, eventArgs) =>
        {
            var messageId = eventArgs.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
            try
            {
                await HandleMessageAsync(eventArgs, stoppingToken);
                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                _logger.LogInformation("Webhook delivered for message {MessageId}.", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook delivery failed after retries for message {MessageId}. Routing to DLQ.", messageId);
                // Nack with requeue: false forwards message to dead letter exchange
                _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(_options.QueueName, autoAck: false, consumer);
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        var eventType = eventArgs.BasicProperties.Type ?? "Unknown";

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MiniBankingDbContext>();

        string? merchantId = null;
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("MerchantId", out var merchantIdElement))
                merchantId = merchantIdElement.GetString();
        }
        catch
        {
            _logger.LogWarning("Failed to parse webhook payload for merchant id.");
        }

        if (string.IsNullOrWhiteSpace(merchantId))
            throw new InvalidOperationException("Merchant id not found in event payload.");

        var merchant = await dbContext.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MerchantId == merchantId, cancellationToken);

        if (merchant is null)
            throw new InvalidOperationException($"Merchant {merchantId} not found.");

        if (string.IsNullOrWhiteSpace(merchant.WebhookUrl))
        {
            _logger.LogInformation("Merchant {MerchantId} has no webhook configured; skipping.", merchantId);
            return;
        }

        var retryPolicy = CreateRetryPolicy();
        var httpClient = _httpClientFactory.CreateClient();

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = HmacSignatureService.ComputeHmac($"{timestamp}.{body}", merchant.Secret);

        var response = await retryPolicy.ExecuteAsync(
            async ct =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, merchant.WebhookUrl)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("X-Event-Type", eventType);
                request.Headers.Add("X-Timestamp", timestamp);
                request.Headers.Add("X-Signature", signature);
                return await httpClient.SendAsync(request, ct);
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Gone)
        {
            throw new HttpRequestException($"Webhook returned {(int)response.StatusCode} {response.StatusCode}.");
        }
    }

    private static AsyncRetryPolicy<HttpResponseMessage> CreateRetryPolicy()
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r =>
                (int)r.StatusCode >= 500 ||
                r.StatusCode == HttpStatusCode.RequestTimeout ||
                r.StatusCode == HttpStatusCode.TooManyRequests)
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, delay, retryCount, context) =>
                {
                    // Retry logging hook
                });
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        base.Dispose();
    }
}
