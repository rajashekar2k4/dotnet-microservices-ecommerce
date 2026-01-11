using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ECommerce.Shared.Infrastructure.Middleware;

/// <summary>
/// Rate limiting middleware for DDoS protection (Security best practice).
/// Follows Single Responsibility Principle - handles rate limiting only.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    private readonly int _requestLimit;
    private readonly TimeSpan _timeWindow;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger,
        int requestLimit = 100,
        int timeWindowSeconds = 60)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _requestLimit = requestLimit;
        _timeWindow = TimeSpan.FromSeconds(timeWindowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientIdentifier(context);

        var clientInfo = _clients.GetOrAdd(clientId, _ => new ClientRequestInfo());

        lock (clientInfo)
        {
            var now = DateTime.UtcNow;

            // Reset if time window has passed
            if (now - clientInfo.WindowStart > _timeWindow)
            {
                clientInfo.RequestCount = 0;
                clientInfo.WindowStart = now;
            }

            clientInfo.RequestCount++;

            if (clientInfo.RequestCount > _requestLimit)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for client {ClientId}. Requests: {RequestCount}",
                    clientId, clientInfo.RequestCount);

                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.Headers["Retry-After"] = _timeWindow.TotalSeconds.ToString();
                return;
            }
        }

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get API key first
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey))
            return $"apikey:{apiKey}";

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }

    private class ClientRequestInfo
    {
        public int RequestCount { get; set; }
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
    }
}
