using ECommerce.Catalog.Domain.Common;

namespace ECommerce.Catalog.Domain.Events;

/// <summary>
/// Domain event raised when a new product is created.
/// </summary>
public record ProductCreatedEvent : IDomainEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; }
    public decimal Price { get; init; }
    public string Category { get; init; }
    public DateTime OccurredOn { get; init; }

    public ProductCreatedEvent(Guid productId, string name, decimal price, string category)
    {
        ProductId = productId;
        Name = name;
        Price = price;
        Category = category;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Domain event raised when product price is changed.
/// </summary>
public record ProductPriceChangedEvent : IDomainEvent
{
    public Guid ProductId { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public DateTime OccurredOn { get; init; }

    public ProductPriceChangedEvent(Guid productId, decimal oldPrice, decimal newPrice)
    {
        ProductId = productId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Domain event raised when product stock is updated.
/// </summary>
public record ProductStockUpdatedEvent : IDomainEvent
{
    public Guid ProductId { get; init; }
    public int OldStock { get; init; }
    public int NewStock { get; init; }
    public DateTime OccurredOn { get; init; }

    public ProductStockUpdatedEvent(Guid productId, int oldStock, int newStock)
    {
        ProductId = productId;
        OldStock = oldStock;
        NewStock = newStock;
        OccurredOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Domain event raised when product goes out of stock.
/// </summary>
public record ProductOutOfStockEvent : IDomainEvent
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; }
    public DateTime OccurredOn { get; init; }

    public ProductOutOfStockEvent(Guid productId, string productName)
    {
        ProductId = productId;
        ProductName = productName;
        OccurredOn = DateTime.UtcNow;
    }
}
