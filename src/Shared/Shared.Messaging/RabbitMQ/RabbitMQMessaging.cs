using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ECommerce.Shared.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ producer implementation for publishing messages.
/// Implements publisher confirms for reliability.
/// </summary>
public interface IRabbitMQProducer
{
    Task PublishAsync<T>(string exchange, string routingKey, T message);
}

public class RabbitMQProducer : IRabbitMQProducer, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQProducer> _logger;

    public RabbitMQProducer(IConfiguration configuration, ILogger<RabbitMQProducer> logger)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Enable publisher confirms for reliability
        _channel.ConfirmSelect();

        _logger = logger;
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        try
        {
            // Declare exchange (idempotent operation)
            _channel.ExchangeDeclare(
                exchange: exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;  // Message persistence
            properties.ContentType = "application/json";
            properties.MessageId = Guid.NewGuid().ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Headers = new Dictionary<string, object>
            {
                { "message-type", typeof(T).Name },
                { "correlation-id", Guid.NewGuid().ToString() }
            };

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            // Wait for publisher confirm
            _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

            _logger.LogInformation(
                "Message published to exchange {Exchange} with routing key {RoutingKey}",
                exchange, routingKey);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to {Exchange}", exchange);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}

/// <summary>
/// RabbitMQ consumer implementation as a background service.
/// Implements message acknowledgment for reliability.
/// </summary>
public class RabbitMQConsumer<T> : BackgroundService where T : class
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQConsumer<T>> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _queueName;

    public RabbitMQConsumer(
        IConfiguration configuration,
        ILogger<RabbitMQConsumer<T>> logger,
        IServiceProvider serviceProvider)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _queueName = $"{typeof(T).Name}-queue";

        // Declare queue with durability
        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Set prefetch count for fair dispatch
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json);

                _logger.LogInformation(
                    "Received message from queue {Queue} with delivery tag {DeliveryTag}",
                    _queueName, ea.DeliveryTag);

                if (message != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<T>>();
                    
                    await handler.HandleAsync(message, stoppingToken);

                    // Acknowledge message after successful processing
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    
                    _logger.LogInformation("Message processed and acknowledged");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");

                // Negative acknowledgment - requeue the message
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: false,  // Manual acknowledgment
            consumer: consumer);

        _logger.LogInformation("RabbitMQ consumer started for queue: {Queue}", _queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}
