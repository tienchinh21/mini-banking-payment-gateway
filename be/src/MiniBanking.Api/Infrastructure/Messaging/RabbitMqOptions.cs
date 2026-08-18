namespace MiniBanking.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "minibanking";
    public string Password { get; set; } = "minibanking_secret";
    public string ExchangeName { get; set; } = "minibanking.events";
    public string QueueName { get; set; } = "minibanking.webhooks";
    public string RoutingKey { get; set; } = "payment.events";
}
