using RabbitMQ.Client;

namespace MiniBanking.Infrastructure.Messaging;

public interface IRabbitMqConnection
{
    bool IsConnected { get; }
    IModel CreateChannel();
    void TryConnect();
}

public class RabbitMqConnection : IRabbitMqConnection, IDisposable
{
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private readonly object _lock = new();
    private bool _disposed;

    public RabbitMqConnection(RabbitMqOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public bool IsConnected => _connection?.IsOpen ?? false;

    public void TryConnect()
    {
        if (IsConnected) return;

        lock (_lock)
        {
            if (IsConnected) return;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true
            };

            _connection = factory.CreateConnection();
        }
    }

    public IModel CreateChannel()
    {
        TryConnect();
        return _connection!.CreateModel();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection?.Dispose();
    }
}
