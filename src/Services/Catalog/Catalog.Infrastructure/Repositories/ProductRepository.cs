using ECommerce.Catalog.Domain.Entities;
using ECommerce.Catalog.Domain.Repositories;
using ECommerce.Catalog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Catalog.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Product entity (Repository Pattern).
/// Follows Single Responsibility Principle - handles only data access.
/// Uses .NET DI with DbContext injected automatically.
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<ProductRepository> _logger;

    /// <summary>
    /// Constructor with .NET dependency injection.
    /// </summary>
    public ProductRepository(CatalogDbContext context, ILogger<ProductRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ========================================================================
    // QUERY OPERATIONS (Read - for CQRS Query side)
    // ========================================================================

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching product with ID: {ProductId}", id);

        return await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching products - Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

        return await _context.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching products in category: {Category}", category);

        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Category == category)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products.CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.AnyAsync(p => p.Id == id, cancellationToken);
    }

    // ========================================================================
    // COMMAND OPERATIONS (Write - for CQRS Command side)
    // ========================================================================

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding product to repository: {ProductId}", product.Id);

        await _context.Products.AddAsync(product, cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating product in repository: {ProductId}", product.Id);

        _context.Products.Update(product);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting product from repository: {ProductId}", id);

        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product != null)
        {
            _context.Products.Remove(product);
        }
    }

    // ========================================================================
    // UNIT OF WORK (Save Changes)
    // ========================================================================

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving changes to database");

        return await _context.SaveChangesAsync(cancellationToken);
    }
}
