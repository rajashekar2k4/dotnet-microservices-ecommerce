using ECommerce.Catalog.Application.DTOs;
using ECommerce.Catalog.Domain.Repositories;
using MediatR;

namespace ECommerce.Catalog.Application.Queries;

// ============================================================================
// GET PRODUCT BY ID QUERY (CQRS - Query Side)
// ============================================================================

/// <summary>
/// Query to get a product by ID (CQRS Read operation).
/// Follows Query pattern - read-only, no side effects.
/// </summary>
public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto?>;

/// <summary>
/// Handler for GetProductByIdQuery (CQRS Query Handler).
/// Uses Repository pattern with .NET DI.
/// Follows Single Responsibility Principle - handles only product retrieval.
/// </summary>
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    /// <summary>
    /// Constructor with .NET dependency injection.
    /// IProductRepository is automatically injected by .NET DI container.
    /// </summary>
    public GetProductByIdQueryHandler(
        IProductRepository repository,
        ILogger<GetProductByIdQueryHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching product with ID: {ProductId}", request.ProductId);

        // Get from repository (Repository Pattern)
        var product = await _repository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product not found with ID: {ProductId}", request.ProductId);
            return null;
        }

        // Map to DTO (Data Transfer Object pattern)
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price.Amount,
            Currency = product.Price.Currency,
            Stock = product.Stock.Quantity,
            Category = product.Category,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            AverageRating = product.Rating.AverageRating,
            TotalRatings = product.Rating.TotalRatings,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}

// ============================================================================
// GET PRODUCTS QUERY (CQRS - Query Side with Pagination)
// ============================================================================

/// <summary>
/// Query to get all products with pagination (CQRS Read operation).
/// </summary>
public record GetProductsQuery : IRequest<PagedResult<ProductDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

/// <summary>
/// Handler for GetProductsQuery (CQRS Query Handler).
/// Uses Repository pattern with .NET DI.
/// </summary>
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    /// <summary>
    /// Constructor with .NET dependency injection.
    /// </summary>
    public GetProductsQueryHandler(
        IProductRepository repository,
        ILogger<GetProductsQueryHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching products - Page: {PageNumber}, Size: {PageSize}",
            request.PageNumber, request.PageSize);

        // Get total count from repository
        var totalCount = await _repository.GetTotalCountAsync(cancellationToken);

        // Get paginated products from repository
        var products = await _repository.GetAllAsync(
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        // Map to DTOs
        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price.Amount,
            Currency = p.Price.Currency,
            Stock = p.Stock.Quantity,
            Category = p.Category,
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive,
            AverageRating = p.Rating.AverageRating,
            TotalRatings = p.Rating.TotalRatings,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        return new PagedResult<ProductDto>
        {
            Items = productDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}

// ============================================================================
// GET PRODUCTS BY CATEGORY QUERY (CQRS - Query Side)
// ============================================================================

/// <summary>
/// Query to get products by category (CQRS Read operation).
/// </summary>
public record GetProductsByCategoryQuery : IRequest<IEnumerable<ProductDto>>
{
    public string Category { get; init; } = string.Empty;
}

/// <summary>
/// Handler for GetProductsByCategoryQuery (CQRS Query Handler).
/// Uses Repository pattern with .NET DI.
/// </summary>
public class GetProductsByCategoryQueryHandler : IRequestHandler<GetProductsByCategoryQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<GetProductsByCategoryQueryHandler> _logger;

    /// <summary>
    /// Constructor with .NET dependency injection.
    /// </summary>
    public GetProductsByCategoryQueryHandler(
        IProductRepository repository,
        ILogger<GetProductsByCategoryQueryHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetProductsByCategoryQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching products in category: {Category}", request.Category);

        // Get from repository
        var products = await _repository.GetByCategoryAsync(request.Category, cancellationToken);

        // Map to DTOs
        return products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price.Amount,
            Currency = p.Price.Currency,
            Stock = p.Stock.Quantity,
            Category = p.Category,
            ImageUrl = p.ImageUrl,
            IsActive = p.IsActive,
            AverageRating = p.Rating.AverageRating,
            TotalRatings = p.Rating.TotalRatings,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();
    }
}
