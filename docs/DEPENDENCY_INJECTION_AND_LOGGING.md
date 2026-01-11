# Dependency Injection & Logging Guide

## Overview

This document explains the dependency injection and logging implementation in the distributed e-commerce solution.

---

## Dependency Injection

### Architecture

The solution uses **constructor-based dependency injection** following .NET best practices:

```
API Layer → Application Layer → Infrastructure Layer → Domain Layer
```

### Layer-Specific DI Configuration

#### 1. **Application Layer** (`Catalog.Application/DependencyInjection.cs`)

Registers:
- ✅ **MediatR** for CQRS pattern
- ✅ **FluentValidation** validators
- ✅ Application services

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // Register FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
```

#### 2. **Infrastructure Layer** (`Catalog.Infrastructure/DependencyInjection.cs`)

Registers:
- ✅ **DbContext** (Entity Framework Core)
- ✅ **Repositories** (Repository Pattern)
- ✅ **Unit of Work** (Unit of Work Pattern)
- ✅ **External services**

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Register DbContext
        services.AddDbContext<CatalogDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });
        });

        // Register repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
```

#### 3. **API Layer** (`Catalog.API/Program.cs`)

Orchestrates all layers:

```csharp
// Add Application Layer
builder.Services.AddApplication();

// Add Infrastructure Layer
builder.Services.AddInfrastructure(connectionString);

// Add Aspire integrations
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");
builder.AddRedisClient("redis-cache");

// Add Message Brokers
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
```

### Service Lifetimes

| Service Type | Lifetime | Reason |
|-------------|----------|---------|
| **DbContext** | Scoped | Per-request database connection |
| **Repositories** | Scoped | Tied to DbContext lifetime |
| **Unit of Work** | Scoped | Manages transaction scope |
| **MediatR Handlers** | Transient | Stateless, created per request |
| **Validators** | Transient | Stateless validation logic |
| **Message Producers** | Singleton | Thread-safe, reusable connections |
| **Loggers** | Singleton | Thread-safe, injected by framework |

### Controller Dependency Injection

```csharp
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    // Constructor injection
    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        _logger.LogInformation("Getting products");
        var query = new GetProductsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
```

---

## Logging

### .NET Logger with Serilog

The solution uses **Serilog** as the logging provider for .NET's `ILogger<T>` interface.

### Configuration

#### Program.cs Setup

```csharp
// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Catalog.API")
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .CreateLogger();

// Use Serilog as the logging provider
builder.Host.UseSerilog();
```

#### appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  }
}
```

### Logging Levels

| Level | Usage | Example |
|-------|-------|---------|
| **Trace** | Very detailed debugging | `_logger.LogTrace("Entering method")` |
| **Debug** | Debugging information | `_logger.LogDebug("Query: {Query}", query)` |
| **Information** | General flow | `_logger.LogInformation("Product created: {Id}", id)` |
| **Warning** | Unexpected but handled | `_logger.LogWarning("Product not found: {Id}", id)` |
| **Error** | Errors and exceptions | `_logger.LogError(ex, "Failed to create product")` |
| **Critical** | Critical failures | `_logger.LogCritical(ex, "Database unavailable")` |

### Structured Logging

```csharp
// ✅ Good - Structured logging
_logger.LogInformation("Creating product: {ProductName} with price {Price}", 
    command.Name, command.Price);

// ❌ Bad - String interpolation
_logger.LogInformation($"Creating product: {command.Name} with price {command.Price}");
```

### Logging in Different Layers

#### Controllers

```csharp
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;

    [HttpPost]
    public async Task<IActionResult> CreateProduct(CreateProductCommand command)
    {
        _logger.LogInformation("Creating product: {ProductName}", command.Name);
        
        try
        {
            var result = await _mediator.Send(command);
            _logger.LogInformation("Product created: {ProductId}", result.Id);
            return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product {ProductName}", command.Name);
            return StatusCode(500, "An error occurred");
        }
    }
}
```

#### Command Handlers

```csharp
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct)
    {
        _logger.LogDebug("Handling CreateProductCommand for {ProductName}", request.Name);

        var product = Product.Create(request.Name, request.Price, request.Stock);
        
        await _unitOfWork.Products.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Product created successfully: {ProductId}", product.Id);

        return MapToDto(product);
    }
}
```

#### Repositories

```csharp
public class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<ProductRepository> _logger;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        _logger.LogDebug("Fetching product {ProductId} from database", id);
        
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found", id);
        }

        return product;
    }
}
```

### Request Logging

```csharp
// In Program.cs
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"]);
    };
});
```

### Centralized Logging with Seq

All logs are sent to **Seq** for centralized viewing:

- **URL**: http://localhost:5341
- **Features**:
  - Structured log search
  - Filtering and querying
  - Real-time monitoring
  - Alerts and dashboards

---

## Best Practices

### Dependency Injection

1. ✅ **Use constructor injection** (not property or method injection)
2. ✅ **Register services in appropriate layer** (Application, Infrastructure)
3. ✅ **Use appropriate lifetimes** (Singleton, Scoped, Transient)
4. ✅ **Validate dependencies** with null checks
5. ✅ **Avoid service locator pattern**
6. ✅ **Keep constructors simple** (no business logic)

### Logging

1. ✅ **Use structured logging** with named parameters
2. ✅ **Log at appropriate levels** (Information, Warning, Error)
3. ✅ **Include context** (IDs, names, values)
4. ✅ **Log exceptions** with full stack traces
5. ✅ **Avoid logging sensitive data** (passwords, tokens)
6. ✅ **Use log scopes** for related operations
7. ✅ **Configure different levels** for different environments

---

## Example: Complete Flow with DI and Logging

```csharp
// 1. API Layer - Controller
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(CreateProductCommand command)
    {
        _logger.LogInformation("API: Creating product {ProductName}", command.Name);
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
    }
}

// 2. Application Layer - Command Handler
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork, ILogger<...> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct)
    {
        _logger.LogDebug("Handler: Processing CreateProductCommand");
        
        var product = Product.Create(request.Name, request.Price, request.Stock);
        await _unitOfWork.Products.AddAsync(product, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Handler: Product created {ProductId}", product.Id);
        return MapToDto(product);
    }
}

// 3. Infrastructure Layer - Repository
public class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(CatalogDbContext context, ILogger<...> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task AddAsync(Product product, CancellationToken ct)
    {
        _logger.LogDebug("Repository: Adding product to database");
        await _context.Products.AddAsync(product, ct);
    }
}
```

---

## Summary

✅ **Dependency Injection**: Constructor-based, layer-specific registration  
✅ **Logging**: .NET ILogger with Serilog provider  
✅ **Structured Logging**: Named parameters, context enrichment  
✅ **Centralized Logging**: Seq for log aggregation  
✅ **Best Practices**: Proper lifetimes, appropriate log levels  

All components follow .NET best practices for dependency injection and logging!
