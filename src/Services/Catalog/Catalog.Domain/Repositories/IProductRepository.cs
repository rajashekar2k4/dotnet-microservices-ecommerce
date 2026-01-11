namespace ECommerce.Catalog.Domain.Repositories;

/// <summary>
/// Repository interface for Product entity (Repository Pattern).
/// Follows Interface Segregation Principle - only essential operations.
/// Follows Dependency Inversion Principle - abstraction in Domain layer.
/// </summary>
public interface IProductRepository
{
    // Query operations (Read)
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    // Command operations (Write)
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    // Save changes (Unit of Work pattern integrated)
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
