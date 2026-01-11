using FluentValidation;
using MediatR;
using ECommerce.Shared.Infrastructure.Results;

namespace ECommerce.Shared.Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior for automatic validation (DRY principle).
/// Follows Single Responsibility Principle - validates requests only.
/// Implements Open/Closed Principle - validation logic is extensible.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            var errors = failures
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            _logger.LogWarning("Validation failed for {RequestType} with {ErrorCount} errors",
                typeof(TRequest).Name, errors.Count);

            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
            
            // Return failure result
            return (TResponse)(object)Result.Failure(errorMessage);
        }

        return await next();
    }
}
