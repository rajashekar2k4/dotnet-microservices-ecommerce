using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ECommerce.Shared.Infrastructure.Middleware;

/// <summary>
/// Security headers middleware for XSS, clickjacking protection (Security best practice).
/// Follows Single Responsibility Principle - adds security headers only.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: Prevents MIME type sniffing
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

        // X-Frame-Options: Prevents clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");

        // X-XSS-Protection: Enables XSS filter
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

        // Strict-Transport-Security: Enforces HTTPS
        context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        // Content-Security-Policy: Prevents XSS attacks
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");

        // Referrer-Policy: Controls referrer information
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions-Policy: Controls browser features
        context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

        _logger.LogDebug("Security headers added to response");

        await _next(context);
    }
}
