using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ECommerce.Shared.Messaging.Kafka;

/// <summary>
/// Kafka producer implementation for publishing messages.
/// Implements idempotent production with retry logic.
/// </summary>
public interface IKafkaProducer
{
    Task ProduceAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
    Task ProduceAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default);
}

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            EnableIdempotence = true,  // Ensures exactly-once semantics
            Acks = Acks.All,            // Wait for all replicas
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000,
            CompressionType = CompressionType.Snappy,
            LingerMs = 10,
            BatchSize = 16384
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                logger.LogError($"Kafka Producer Error: {error.Reason}"))
            .SetLogHandler((_, logMessage) =>
                logger.LogDebug($"Kafka Log: {logMessage.Message}"))
            .Build();

        _logger = logger;
    }

    public async Task ProduceAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        await ProduceAsync(topic, Guid.NewGuid().ToString(), message, cancellationToken);
    }

    public async Task ProduceAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<string, string>
            {
                Key = key,
                Value = json,
                Timestamp = new Timestamp(DateTime.UtcNow),
                Headers = new Headers
                {
                    { "message-type", System.Text.Encoding.UTF8.GetBytes(typeof(T).Name) },
                    { "correlation-id", System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) }
                }
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

            _logger.LogInformation(
                "Message delivered to {Topic} [{Partition}] at offset {Offset} | Key: {Key}",
                result.Topic, result.Partition.Value, result.Offset.Value, key);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to deliver message to {Topic}: {Reason}", topic, ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error producing message to {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}

/// <summary>
/// Kafka consumer implementation as a background service.
/// Implements at-least-once delivery with manual commit.
/// </summary>
public interface IMessageHandler<T>
{
    Task HandleAsync(T message, CancellationToken cancellationToken);
}

public class KafkaConsumer<T> : BackgroundService where T : class
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumer<T>> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _topic;

    public KafkaConsumer(
        IConfiguration configuration,
        ILogger<KafkaConsumer<T>> logger,
        IServiceProvider serviceProvider)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = $"{typeof(T).Name}-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,  // Manual commit for reliability
            EnablePartitionEof = true,
            SessionTimeoutMs = 30000,
            MaxPollIntervalMs = 300000
        };

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
                logger.LogError("Kafka Consumer Error: {Reason}", error.Reason))
            .SetPartitionsAssignedHandler((_, partitions) =>
                logger.LogInformation("Partitions assigned: {Partitions}",
                    string.Join(", ", partitions)))
            .Build();

        _logger = logger;
        _serviceProvider = serviceProvider;
        _topic = typeof(T).Name.ToLowerInvariant();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);
        _logger.LogInformation("Kafka consumer started for topic: {Topic}", _topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);

                if (consumeResult.IsPartitionEOF)
                {
                    _logger.LogDebug("Reached end of partition {Partition}", consumeResult.Partition);
                    continue;
                }

                _logger.LogInformation(
                    "Received message from {Topic} [{Partition}] at offset {Offset}",
                    consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);

                var message = JsonSerializer.Deserialize<T>(consumeResult.Message.Value);

                if (message != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<T>>();
                    
                    await handler.HandleAsync(message, stoppingToken);

                    // Commit offset after successful processing
                    _consumer.Commit(consumeResult);
                    
                    _logger.LogInformation("Message processed successfully");
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming message: {Reason}", ex.Error.Reason);
                
                // Don't commit on error - message will be reprocessed
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                
                // Implement dead letter queue logic here if needed
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _consumer.Close();
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
