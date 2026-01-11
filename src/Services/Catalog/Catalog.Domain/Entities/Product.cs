using ECommerce.Catalog.Domain.Common;
using ECommerce.Catalog.Domain.Events;
using ECommerce.Catalog.Domain.ValueObjects;

namespace ECommerce.Catalog.Domain.Entities;

/// <summary>
/// Product aggregate root with rich domain logic.
/// Implements DDD patterns: Entity, Aggregate Root, Value Objects, Domain Events.
/// </summary>
public class Product : Entity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public StockQuantity Stock { get; private set; }
    public string Category { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }
    public ProductRating Rating { get; private set; }

    // EF Core constructor
    private Product() { }

    private Product(string name, string description, Money price, StockQuantity stock, string category)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Price = price ?? throw new ArgumentNullException(nameof(price));
        Stock = stock ?? throw new ArgumentNullException(nameof(stock));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        IsActive = true;
        Rating = new ProductRating();
    }

    /// <summary>
    /// Factory method to create a new product (Factory Pattern).
    /// </summary>
    public static Product Create(
        string name, 
        string description, 
        decimal price, 
        int stock, 
        string category,
        string? imageUrl = null)
    {
        var product = new Product(
            name,
            description,
            new Money(price),
            new StockQuantity(stock),
            category)
        {
            ImageUrl = imageUrl
        };

        // Raise domain event
        product.AddDomainEvent(new ProductCreatedEvent(
            product.Id,
            product.Name,
            product.Price.Amount,
            product.Category));

        return product;
    }

    /// <summary>
    /// Updates product price with business validation.
    /// </summary>
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(newPrice));

        var oldPrice = Price.Amount;
        Price = new Money(newPrice, Price.Currency);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductPriceChangedEvent(Id, oldPrice, newPrice));
    }

    /// <summary>
    /// Updates product stock quantity.
    /// </summary>
    public void UpdateStock(int quantity)
    {
        var oldStock = Stock.Quantity;
        Stock = new StockQuantity(quantity);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductStockUpdatedEvent(Id, oldStock, quantity));

        if (quantity == 0)
        {
            AddDomainEvent(new ProductOutOfStockEvent(Id, Name));
        }
    }

    /// <summary>
    /// Decreases stock when product is ordered.
    /// </summary>
    public void DecreaseStock(int quantity)
    {
        var oldStock = Stock.Quantity;
        Stock = Stock.Decrease(quantity);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductStockUpdatedEvent(Id, oldStock, Stock.Quantity));

        if (Stock.Quantity == 0)
        {
            AddDomainEvent(new ProductOutOfStockEvent(Id, Name));
        }
    }

    /// <summary>
    /// Increases stock when inventory is replenished.
    /// </summary>
    public void IncreaseStock(int quantity)
    {
        var oldStock = Stock.Quantity;
        Stock = Stock.Increase(quantity);
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new ProductStockUpdatedEvent(Id, oldStock, Stock.Quantity));
    }

    /// <summary>
    /// Updates product information.
    /// </summary>
    public void UpdateInfo(string name, string description, string category, string? imageUrl = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a rating to the product.
    /// </summary>
    public void AddRating(int stars, string? review = null)
    {
        Rating = Rating.AddRating(stars, review);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the product.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the product.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Value object for product rating.
/// </summary>
public record ProductRating
{
    public double AverageRating { get; init; }
    public int TotalRatings { get; init; }

    public ProductRating()
    {
        AverageRating = 0;
        TotalRatings = 0;
    }

    private ProductRating(double averageRating, int totalRatings)
    {
        AverageRating = averageRating;
        TotalRatings = totalRatings;
    }

    public ProductRating AddRating(int stars, string? review = null)
    {
        if (stars < 1 || stars > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(stars));

        var newTotal = TotalRatings + 1;
        var newAverage = ((AverageRating * TotalRatings) + stars) / newTotal;

        return new ProductRating(Math.Round(newAverage, 2), newTotal);
    }
}
