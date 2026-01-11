# .NET 10 Dependency Injection & Logging Guide

## Overview

This guide demonstrates the use of **.NET 10's built-in dependency injection** and **ILogger** without any third-party libraries.

---

## Dependency Injection

### .NET 10 Default DI Container

.NET 10 includes a powerful built-in dependency injection container that supports:
- ✅ Constructor injection
- ✅ Service lifetimes (Singleton, Scoped, Transient)
- ✅ Service registration
- ✅ Automatic disposal

### Service Registration

#### Application Layer

```csharp
// Catalog.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR for CQRS
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
```

#### Infrastructure Layer

```csharp
// Catalog.Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Register DbContext (Scoped lifetime)
        services.AddDbContext<CatalogDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // Register repositories (Scoped lifetime)
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
```

#### API Layer (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Application Layer
builder.Services.AddApplication();

// Add Infrastructure Layer
builder.Services.AddInfrastructure(connectionString);

// Add Singleton services (shared across all requests)
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();

// Add Scoped services (per request)
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Add Transient services (new instance every time)
builder.Services.AddTransient<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
```

### Service Lifetimes

| Lifetime | Description | Use Case |
|----------|-------------|----------|
| **Singleton** | Single instance for application lifetime | Stateless services, caches, message producers |
| **Scoped** | One instance per request | DbContext, repositories, Unit of Work |
| **Transient** | New instance every time | Validators, lightweight services |

### Constructor Injection

```csharp
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    // .NET DI automatically injects dependencies
    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

---

## Logging with ILogger

### .NET 10 Built-in Logging

.NET 10 includes a comprehensive logging framework with:
- ✅ Multiple logging providers (Console, Debug, EventSource)
- ✅ Structured logging support
- ✅ Log levels (Trace, Debug, Information, Warning, Error, Critical)
- ✅ Log scopes
- ✅ Configuration via appsettings.json

### Configuration (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Clear default providers
builder.Logging.ClearProviders();

// Add logging providers
builder.Logging.AddConsole();        // Console output
builder.Logging.AddDebug();          // Debug window
builder.Logging.AddEventSourceLogger(); // Event source

// Add JSON console logging for structured logs
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.JsonWriterOptions = new JsonWriterOptions
    {
        Indented = true
    };
});

// Configure from appsettings.json
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
```

### Configuration (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    },
    "Console": {
      "FormatterName": "simple",
      "FormatterOptions": {
        "SingleLine": true,
        "IncludeScopes": true,
        "TimestampFormat": "yyyy-MM-dd HH:mm:ss "
      }
    }
  }
}
```

### Using ILogger

#### Inject ILogger

```csharp
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ILogger<ProductsController> logger)
    {
        _logger = logger;
    }
}
```

#### Log Levels

```csharp
// Trace - Very detailed debugging
_logger.LogTrace("Entering GetProduct method with ID: {ProductId}", id);

// Debug - Debugging information
_logger.LogDebug("Query executed: {Query}", query);

// Information - General flow
_logger.LogInformation("Product created successfully with ID: {ProductId}", id);

// Warning - Unexpected but handled
_logger.LogWarning("Product not found with ID: {ProductId}", id);

// Error - Errors and exceptions
_logger.LogError(ex, "Error occurred while creating product");

// Critical - Critical failures
_logger.LogCritical(ex, "Database connection failed");
```

#### Structured Logging

```csharp
// ✅ GOOD - Structured logging with named parameters
_logger.LogInformation("Creating product: {ProductName} with price {Price}", 
    command.Name, command.Price);

// ❌ BAD - String interpolation (not structured)
_logger.LogInformation($"Creating product: {command.Name} with price {command.Price}");
```

#### Log Scopes

```csharp
using (_logger.BeginScope("OrderId: {OrderId}", orderId))
{
    _logger.LogInformation("Processing order");
    _logger.LogInformation("Validating items");
    // All logs in this scope will include OrderId
}
```

### Custom Request Logging Middleware

```csharp
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("HTTP {Method} {Path} started", 
        context.Request.Method, context.Request.Path);
    
    var sw = Stopwatch.StartNew();
    await next();
    sw.Stop();
    
    logger.LogInformation("HTTP {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
        context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds, context.Response.StatusCode);
});
```

---

## Complete Example

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddJsonConsole();

// Dependency Injection
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();

var app = builder.Build();

// Get logger
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Custom middleware
app.Use(async (context, next) =>
{
    var requestLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    requestLogger.LogInformation("Request: {Method} {Path}", 
        context.Request.Method, context.Request.Path);
    await next();
});

logger.LogInformation("Application started");
app.Run();
```

### Controller

```csharp
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
        _logger.LogInformation("Creating product: {Name}", command.Name);
        
        try
        {
            var result = await _mediator.Send(command);
            _logger.LogInformation("Product created: {Id}", result.Id);
            return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500);
        }
    }
}
```

---

## Best Practices

### Dependency Injection

1. ✅ Use constructor injection
2. ✅ Register services in extension methods
3. ✅ Use appropriate lifetimes
4. ✅ Validate dependencies (null checks)
5. ✅ Avoid service locator pattern
6. ✅ Keep constructors simple

### Logging

1. ✅ Use structured logging (named parameters)
2. ✅ Log at appropriate levels
3. ✅ Include context (IDs, names)
4. ✅ Log exceptions with full details
5. ✅ Avoid logging sensitive data
6. ✅ Use log scopes for related operations
7. ✅ Configure levels per namespace

---

## Summary

✅ **.NET 10 Default DI**: Built-in, powerful, no third-party libraries needed  
✅ **ILogger**: Structured logging, multiple providers, configuration-based  
✅ **Constructor Injection**: Clean, testable, explicit dependencies  
✅ **Service Lifetimes**: Singleton, Scoped, Transient  
✅ **Structured Logging**: Named parameters, searchable, analyzable  

No Serilog or other third-party libraries required!
