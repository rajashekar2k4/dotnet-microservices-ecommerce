using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ECommerce.Shared.Infrastructure.Results;
using System.Net;
using System.Text.Json;

namespace ECommerce.Shared.Infrastructure.Middleware;

/// <summary>
/// Global exception handling middleware (DRY principle).
/// Centralized error handling for consistent API responses.
/// Follows Single Responsibility Principle - handles exceptions only.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();

        _logger.LogError(exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}", correlationId);

        var (statusCode, message) = exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "A required value was not provided"),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "The requested resource was not found"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "An internal server error occurred")
        };

        var errorResponse = new ErrorResponse
        {
            Message = message,
            Details = context.Request.Host.Host.Contains("localhost") ? exception.StackTrace : null,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
