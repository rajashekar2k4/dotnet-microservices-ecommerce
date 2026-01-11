namespace ECommerce.Catalog.Domain.ValueObjects;

/// <summary>
/// Value Object representing money with currency support.
/// Implements equality by value, not reference (DDD pattern).
/// </summary>
public record Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");

        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies");

        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money money, decimal multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}

/// <summary>
/// Value Object for product stock quantity.
/// </summary>
public record StockQuantity
{
    public int Quantity { get; init; }

    public StockQuantity(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(quantity));

        Quantity = quantity;
    }

    public bool IsInStock => Quantity > 0;
    public bool IsLowStock(int threshold = 10) => Quantity > 0 && Quantity <= threshold;

    public StockQuantity Increase(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        return new StockQuantity(Quantity + amount);
    }

    public StockQuantity Decrease(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        if (amount > Quantity)
            throw new InvalidOperationException("Insufficient stock");

        return new StockQuantity(Quantity - amount);
    }

    public static implicit operator int(StockQuantity stock) => stock.Quantity;
}
