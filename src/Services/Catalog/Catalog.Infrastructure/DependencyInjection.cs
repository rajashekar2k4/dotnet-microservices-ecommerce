using ECommerce.Catalog.Domain.Repositories;
using ECommerce.Catalog.Infrastructure.Data;
using ECommerce.Catalog.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Catalog.Infrastructure;

/// <summary>
/// Dependency Injection configuration for Infrastructure layer.
/// Uses .NET 10 default DI with Repository pattern.
/// Follows Dependency Inversion Principle - registers implementations.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Register DbContext with .NET DI (Scoped lifetime by default)
        services.AddDbContext<CatalogDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
                npgsqlOptions.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName);
            });
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });

        // Register Repository Pattern with .NET DI (Scoped lifetime)
        // Scoped = one instance per HTTP request (recommended for repositories)
        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}
