using ECommerce.Catalog.Application;
using ECommerce.Catalog.Infrastructure;
using ECommerce.Shared.Messaging.Kafka;
using ECommerce.Shared.Messaging.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// LOGGING CONFIGURATION (.NET 10 Default ILogger)
// ============================================================================

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

// Configure logging levels from appsettings.json
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// Optional: Add JSON console logging for structured logs
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = true
    };
});

// ============================================================================
// ASPIRE SERVICE DEFAULTS (includes OpenTelemetry, Health Checks, Resilience)
// ============================================================================

builder.AddServiceDefaults();

// ============================================================================
// DEPENDENCY INJECTION (.NET 10 Default DI Container)
// ============================================================================

// Add Application Layer (CQRS, MediatR, Validators)
builder.Services.AddApplication();

// Add Infrastructure Layer (DbContext, Repositories, Unit of Work)
var catalogConnectionString = builder.Configuration.GetConnectionString("catalogdb")
    ?? throw new InvalidOperationException("Connection string 'catalogdb' not found.");

builder.Services.AddInfrastructure(catalogConnectionString);

// Add PostgreSQL with Aspire
builder.AddNpgsqlDbContext<ECommerce.Catalog.Infrastructure.Data.CatalogDbContext>("catalogdb");

// Add Redis with Aspire
builder.AddRedisClient("redis-cache");

// Add Message Brokers (Singleton lifetime for connection reuse)
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();

// ============================================================================
// API CONFIGURATION
// ============================================================================

// Add Controllers
builder.Services.AddControllers();

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "E-Commerce Catalog API",
        Version = "v1",
        Description = "Catalog microservice for product management with CQRS pattern",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "API Support",
            Email = "support@ecommerce.com"
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================================================================
// BUILD APPLICATION
// ============================================================================

var app = builder.Build();

// Get logger for application startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// ============================================================================
// MIDDLEWARE PIPELINE
// ============================================================================

// Configure Swagger (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
        options.RoutePrefix = string.Empty; // Swagger at root
    });
    
    logger.LogInformation("Swagger UI enabled at root URL");
}

// Custom request logging middleware
app.Use(async (context, next) =>
{
    var requestLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    requestLogger.LogInformation("HTTP {Method} {Path} started", 
        context.Request.Method, context.Request.Path);
    
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await next();
    sw.Stop();
    
    requestLogger.LogInformation("HTTP {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
        context.Request.Method, context.Request.Path, sw.ElapsedMilliseconds, context.Response.StatusCode);
});

// HTTPS Redirection
app.UseHttpsRedirection();

// CORS
app.UseCors();

// Authentication & Authorization (add when implemented)
// app.UseAuthentication();
// app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Map Aspire default endpoints (health checks)
app.MapDefaultEndpoints();

// ============================================================================
// RUN APPLICATION
// ============================================================================

try
{
    logger.LogInformation("Starting Catalog API on {Environment}", app.Environment.EnvironmentName);
    logger.LogInformation("Application started successfully");
    
    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application terminated unexpectedly");
    throw;
}
