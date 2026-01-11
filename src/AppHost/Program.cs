using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// ============================================================================
// INFRASTRUCTURE RESOURCES
// ============================================================================

// Redis for distributed caching
var redis = builder.AddRedis("redis-cache")
    .WithRedisCommander()
    .WithDataVolume();

// PostgreSQL databases for microservices
var catalogDb = builder.AddPostgres("postgres-catalog")
    .WithPgAdmin()
    .AddDatabase("catalogdb");

var orderingDb = builder.AddPostgres("postgres-ordering")
    .AddDatabase("orderingdb");

// Kafka for event-driven messaging
var kafka = builder.AddKafka("kafka")
    .WithKafkaUI()
    .WithDataVolume();

// RabbitMQ for message queuing
var rabbitMq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithDataVolume();

// EventStoreDB for event sourcing
var eventStore = builder.AddContainer("eventstore", "eventstore/eventstore", "22.10.0-alpine")
    .WithHttpEndpoint(port: 2113, targetPort: 2113, name: "http")
    .WithEndpoint(port: 1113, targetPort: 1113, name: "tcp")
    .WithEnvironment("EVENTSTORE_CLUSTER_SIZE", "1")
    .WithEnvironment("EVENTSTORE_RUN_PROJECTIONS", "All")
    .WithEnvironment("EVENTSTORE_START_STANDARD_PROJECTIONS", "true")
    .WithEnvironment("EVENTSTORE_INSECURE", "true")
    .WithEnvironment("EVENTSTORE_ENABLE_EXTERNAL_TCP", "true")
    .WithEnvironment("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", "true");

// Seq for centralized logging
var seq = builder.AddSeq("seq");

// ============================================================================
// MICROSERVICES
// ============================================================================

// Catalog Microservice (CQRS Pattern)
var catalogApi = builder.AddProject<Projects.ECommerce_Catalog_API>("catalog-api")
    .WithReference(catalogDb)
    .WithReference(redis)
    .WithReference(kafka)
    .WithReference(rabbitMq)
    .WithReference(seq)
    .WithReplicas(2);

// Ordering Microservice (Event Sourcing)
var orderingApi = builder.AddProject<Projects.ECommerce_Ordering_API>("ordering-api")
    .WithReference(orderingDb)
    .WithReference(redis)
    .WithReference(kafka)
    .WithReference(rabbitMq)
    .WithReference(eventStore)
    .WithReference(seq)
    .WithReplicas(2);

// Payment Microservice
var paymentApi = builder.AddProject<Projects.ECommerce_Payment_API>("payment-api")
    .WithReference(kafka)
    .WithReference(rabbitMq)
    .WithReference(seq);

// Notification Microservice
var notificationApi = builder.AddProject<Projects.ECommerce_Notification_API>("notification-api")
    .WithReference(kafka)
    .WithReference(rabbitMq)
    .WithReference(seq);

// AI Agents Microservice (Azure OpenAI Integration)
var aiAgents = builder.AddProject<Projects.ECommerce_AI_Agents_API>("ai-agents")
    .WithReference(redis)
    .WithReference(seq);

// ============================================================================
// API GATEWAY (YARP)
// ============================================================================

var apiGateway = builder.AddProject<Projects.ECommerce_ApiGateway>("api-gateway")
    .WithReference(catalogApi)
    .WithReference(orderingApi)
    .WithReference(paymentApi)
    .WithReference(notificationApi)
    .WithReference(aiAgents)
    .WithReference(seq)
    .WithExternalHttpEndpoints();

// ============================================================================
// BUILD AND RUN
// ============================================================================

builder.Build().Run();
