using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ECommerce.Shared.Infrastructure.Results;

namespace ECommerce.Shared.Infrastructure.Controllers;

/// <summary>
/// Base controller with common functionality (DRY principle).
/// Follows Single Responsibility Principle - handles common API concerns.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected readonly ILogger Logger;

    protected BaseApiController(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns OK result with value (DRY - eliminates duplicate Ok() calls).
    /// </summary>
    protected IActionResult OkResult<T>(T value)
    {
        return Ok(value);
    }

    /// <summary>
    /// Returns result based on Result pattern (DRY principle).
    /// </summary>
    protected IActionResult FromResult(Result result)
    {
        if (result.IsSuccess)
            return Ok();

        return BadRequest(new ErrorResponse
        {
            Message = result.Error,
            CorrelationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Returns result with value based on Result pattern (DRY principle).
    /// </summary>
    protected IActionResult FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return BadRequest(new ErrorResponse
        {
            Message = result.Error,
            CorrelationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Returns not found result with consistent error format (DRY principle).
    /// </summary>
    protected IActionResult NotFoundResult(string message)
    {
        Logger.LogWarning("Resource not found: {Message}", message);
        
        return NotFound(new ErrorResponse
        {
            Message = message,
            CorrelationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Returns bad request with validation errors (DRY principle).
    /// </summary>
    protected IActionResult ValidationError(Dictionary<string, string[]> errors)
    {
        Logger.LogWarning("Validation failed with {ErrorCount} errors", errors.Count);
        
        return BadRequest(new ErrorResponse
        {
            Message = "Validation failed",
            ValidationErrors = errors,
            CorrelationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Returns internal server error with consistent format (DRY principle).
    /// </summary>
    protected IActionResult InternalServerError(string message, Exception? exception = null)
    {
        Logger.LogError(exception, "Internal server error: {Message}", message);
        
        return StatusCode(500, new ErrorResponse
        {
            Message = "An internal server error occurred",
            Details = exception?.Message,
            CorrelationId = GetCorrelationId()
        });
    }

    /// <summary>
    /// Gets correlation ID from request headers or generates new one (KISS principle).
    /// </summary>
    protected string GetCorrelationId()
    {
        return HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Gets user ID from claims (DRY principle).
    /// </summary>
    protected string? GetUserId()
    {
        return User.FindFirst("sub")?.Value 
            ?? User.FindFirst("userId")?.Value;
    }

    /// <summary>
    /// Checks if user has role (DRY principle).
    /// </summary>
    protected bool HasRole(string role)
    {
        return User.IsInRole(role);
    }
}
