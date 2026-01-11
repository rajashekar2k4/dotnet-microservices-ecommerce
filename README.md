# Distributed E-Commerce System

> A production-ready distributed e-commerce system built with .NET 8, .NET Aspire, microservices architecture, CQRS pattern, and event-driven design.

## 🚀 Features

- **Microservices Architecture** with clean separation of concerns
- **Clean Architecture** (Domain, Application, Infrastructure, API layers)
- **CQRS Pattern** with MediatR for optimized read/write operations
- **Event-Driven Communication** using Kafka and RabbitMQ
- **Entity Framework Core** with PostgreSQL
- **Distributed Caching** with Redis
- **Event Sourcing** for order management
- **AI Integration** with Azure OpenAI
- **Comprehensive Observability** with OpenTelemetry, Seq, Zipkin, Prometheus, Grafana

## 📋 Prerequisites

- .NET 8.0 SDK or later
- Docker Desktop
- Visual Studio 2022 or VS Code
- Azure subscription (optional, for Azure services)

## 🏗️ Architecture

### Microservices

| Service | Description | Technology |
|---------|-------------|------------|
| **Catalog** | Product management, inventory | CQRS, EF Core, PostgreSQL |
| **Ordering** | Order processing, event sourcing | EventStoreDB, PostgreSQL |
| **Payment** | Payment processing | Saga Pattern |
| **Notification** | Email, SMS notifications | Event-driven |
| **AI Agents** | Recommendations, fraud detection | Azure OpenAI |

### Technology Stack

- **.NET 8.0** - Application framework
- **.NET Aspire** - Cloud-native orchestration
- **PostgreSQL** - Primary database
- **Redis** - Distributed caching
- **Kafka** - Event streaming
- **RabbitMQ** - Message queuing
- **EventStoreDB** - Event sourcing
- **MediatR** - CQRS implementation
- **Entity Framework Core** - ORM
- **Polly** - Resilience patterns
- **OpenTelemetry** - Observability

## 🚦 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/distributed-ecommerce.git
cd distributed-ecommerce
```

### 2. Start Infrastructure Services

```bash
docker-compose up -d
```

This will start:
- PostgreSQL (Catalog & Ordering databases)
- Redis
- Kafka + Zookeeper
- RabbitMQ
- EventStoreDB
- Seq (logging)
- Zipkin (tracing)
- Prometheus (metrics)
- Grafana (visualization)

### 3. Run the Application

```bash
cd src/AppHost
dotnet run
```

Access the Aspire Dashboard at: http://localhost:15000

### 4. Access Services

- **API Gateway**: http://localhost:5000
- **Catalog API**: http://localhost:5001
- **Ordering API**: http://localhost:5002
- **Kafka UI**: http://localhost:8080
- **RabbitMQ Management**: http://localhost:15672 (admin/Password123!)
- **Seq Logs**: http://localhost:8081
- **Zipkin Tracing**: http://localhost:9411
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000 (admin/admin)

## 📁 Project Structure

```
DistributedECommerce/
├── src/
│   ├── AppHost/                      # .NET Aspire Orchestration
│   ├── ServiceDefaults/              # Shared Aspire defaults
│   ├── ApiGateway/                   # YARP API Gateway
│   ├── Services/
│   │   ├── Catalog/
│   │   │   ├── Catalog.Domain/      # Domain entities, value objects
│   │   │   ├── Catalog.Application/ # CQRS commands/queries
│   │   │   ├── Catalog.Infrastructure/ # EF Core, repositories
│   │   │   └── Catalog.API/         # REST API
│   │   ├── Ordering/                # Ordering microservice
│   │   ├── Payment/                 # Payment microservice
│   │   ├── Notification/            # Notification microservice
│   │   └── AI.Agents/               # AI agents microservice
│   └── Shared/
│       ├── Shared.Contracts/        # DTOs, events
│       ├── Shared.Messaging/        # Kafka, RabbitMQ
│       └── Shared.Infrastructure/   # Common utilities
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
├── docs/
│   └── comprehensive_documentation.md
├── docker-compose.yml
└── README.md
```

## 🎯 Design Patterns Implemented

- ✅ **CQRS** - Command Query Responsibility Segregation
- ✅ **Repository Pattern** - Data access abstraction
- ✅ **Unit of Work** - Transaction management
- ✅ **Factory Pattern** - Object creation
- ✅ **Mediator Pattern** - MediatR for decoupling
- ✅ **Strategy Pattern** - Interchangeable algorithms
- ✅ **Circuit Breaker** - Fault tolerance (Polly)
- ✅ **Cache-Aside** - Caching strategy
- ✅ **Event Sourcing** - Audit trail
- ✅ **Saga Pattern** - Distributed transactions

## 📊 Clean Architecture Layers

### Domain Layer
- Entities with rich business logic
- Value objects (Money, StockQuantity)
- Domain events
- Repository interfaces

### Application Layer
- CQRS commands and queries
- Command/query handlers (MediatR)
- DTOs
- Validators (FluentValidation)

### Infrastructure Layer
- EF Core DbContext
- Repository implementations
- Message broker integration
- External service clients

### API Layer
- REST controllers
- API versioning
- Swagger/OpenAPI
- Middleware

## 🔄 Message Brokers

### Kafka
- Event streaming
- High throughput
- Partition-level ordering
- Used for: Product events, order events

### RabbitMQ
- Task queues
- Request/reply patterns
- Queue-level ordering
- Used for: Notifications, background jobs

## 💾 Database

### PostgreSQL
- Catalog database (products, categories)
- Ordering database (orders, order items)

### Redis
- Distributed caching
- Session storage
- Cache-aside pattern

### EventStoreDB
- Event sourcing for orders
- Complete audit trail
- Event replay capability

## 🔍 Observability

### Logging (Seq)
- Structured logging
- Centralized log aggregation
- Search and filtering

### Tracing (Zipkin)
- Distributed tracing
- Request flow visualization
- Performance analysis

### Metrics (Prometheus + Grafana)
- Application metrics
- Infrastructure metrics
- Business metrics
- Custom dashboards

## 🧪 Testing

### Unit Tests
```bash
dotnet test tests/UnitTests
```

### Integration Tests
```bash
docker-compose -f docker-compose.test.yml up -d
dotnet test tests/IntegrationTests
docker-compose -f docker-compose.test.yml down
```

## 🚀 Deployment

### Docker
```bash
docker build -t ecommerce/catalog-api:latest -f src/Services/Catalog/Catalog.API/Dockerfile .
docker run -p 5001:8080 ecommerce/catalog-api:latest
```

### Kubernetes
```bash
kubectl apply -f k8s/
kubectl rollout status deployment/catalog-api
```

### Azure
```bash
az deployment group create -f infrastructure/azure-deploy.bicep
az aks get-credentials --resource-group myRG --name myAKS
kubectl apply -f k8s/
```

## 📚 Documentation

Comprehensive documentation is available in:
- [Complete Technical Documentation](docs/comprehensive_documentation.md)
- [Implementation Plan](docs/implementation_plan.md)
- [API Documentation](http://localhost:5000/swagger) (when running)

## 🔐 Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "CatalogDb": "Host=localhost;Database=catalogdb;Username=admin;Password=Password123!",
    "Redis": "localhost:6379"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "admin",
    "Password": "Password123!"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4"
  }
}
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License.

## 👥 Authors

- Development Team

## 🙏 Acknowledgments

- .NET Aspire team
- MediatR library
- Confluent Kafka
- RabbitMQ team
- EventStore team

---

**Built with ❤️ using .NET 8 and .NET Aspire**
