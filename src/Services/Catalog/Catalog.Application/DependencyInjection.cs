using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ECommerce.Catalog.Application;

/// <summary>
/// Dependency Injection configuration for Application layer.
/// Registers MediatR, FluentValidation, and application services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR for CQRS pattern
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register AutoMapper (if needed)
        // services.AddAutoMapper(assembly);

        return services;
    }
}
