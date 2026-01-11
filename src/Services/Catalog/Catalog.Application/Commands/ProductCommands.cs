using ECommerce.Catalog.Application.DTOs;
using ECommerce.Catalog.Domain.Entities;
using ECommerce.Catalog.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace ECommerce.Catalog.Application.Commands;

// ============================================================================
// CREATE PRODUCT COMMAND (CQRS - Command Side)
// ============================================================================

/// <summary>
/// Command to create a new product (CQRS Write operation).
/// Follows Command pattern - represents intent to change state.
/// </summary>
public record CreateProductCommand : IRequest<ProductDto>
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public int Stock { get; init; }
    public string Category { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
}

/// <summary>
/// Validator for CreateProductCommand (Input validation - Security best practice).
/// Follows Single Responsibility Principle - validates only.
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Product description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero")
            .LessThan(1000000).WithMessage("Price must be less than 1,000,000");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required");
    }
}

/// <summary>
/// Handler for CreateProductCommand (CQRS Command Handler).
/// Uses Repository pattern with .NET DI.
/// Follows Single Responsibility Principle - handles only product creation.
/// </summary>
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    /// <summary>
    /// Constructor with .NET dependency injection.
    /// IProductRepository is automatically injected by .NET DI container.
    /// </summary>
    public CreateProductCommandHandler(
        IProductRepository repository,
        ILogger<CreateProductCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating product: {ProductName}", request.Name);

        // Create product using factory method (Factory Pattern + DDD)
        var product = Product.Create(
            request.Name,
            request.Price,
            request.Stock,
            request.Category,
            request.Description,
            request.ImageUrl,
            request.Currency);

        // Add to repository (Repository Pattern)
        await _repository.AddAsync(product, cancellationToken);

        // Save changes (Unit of Work integrated in repository)
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created successfully with ID: {ProductId}", product.Id);

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
// UPDATE PRODUCT PRICE COMMAND (CQRS - Command Side)
// ============================================================================

/// <summary>
/// Command to update product price (CQRS Write operation).
/// </summary>
public record UpdateProductPriceCommand : IRequest<ProductDto>
{
    public Guid ProductId { get; init; }
    public decimal NewPrice { get; init; }
}

/// <summary>
/// Validator for UpdateProductPriceCommand.
/// </summary>
public class UpdateProductPriceCommandValidator : AbstractValidator<UpdateProductPriceCommand>
{
    public UpdateProductPriceCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.NewPrice)
            .GreaterThan(0).WithMessage("Price must be greater than zero")
            .LessThan(1000000).WithMessage("Price must be less than 1,000,000");
    }
}

/// <summary>
/// Handler for UpdateProductPriceCommand (CQRS Command Handler).
/// Uses Repository pattern with .NET DI.
/// </summary>
public class UpdateProductPriceCommandHandler : IRequestHandler<UpdateProductPriceCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<UpdateProductPriceCommandHandler> _logger;

    /// <summary>
    /// Constructor with .NET dependency injection.
    /// </summary>
    public UpdateProductPriceCommandHandler(
        IProductRepository repository,
        ILogger<UpdateProductPriceCommandHandler> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductDto> Handle(UpdateProductPriceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating price for product {ProductId} to {NewPrice}",
            request.ProductId, request.NewPrice);

        // Get product from repository
        var product = await _repository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {request.ProductId} not found");
        }

        // Update price (Domain logic in entity)
        product.UpdatePrice(request.NewPrice);

        // Update in repository
        await _repository.UpdateAsync(product, cancellationToken);

        // Save changes
        await _repository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Price updated successfully for product {ProductId}", request.ProductId);

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
