using ECommerce.Catalog.Domain.Entities;
using ECommerce.Catalog.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ECommerce.Catalog.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for Catalog microservice.
/// Implements persistence for Product aggregate with proper configurations.
/// </summary>
public class CatalogDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.Id)
                .ValueGeneratedNever();

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(p => p.Category)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.ImageUrl)
                .HasMaxLength(500);

            entity.Property(p => p.IsActive)
                .IsRequired();

            entity.Property(p => p.CreatedAt)
                .IsRequired();

            entity.Property(p => p.UpdatedAt);

            // Configure Money value object (Owned Entity Pattern)
            entity.OwnsOne(p => p.Price, price =>
            {
                price.Property(m => m.Amount)
                    .HasColumnName("Price")
                    .HasPrecision(18, 2)
                    .IsRequired();

                price.Property(m => m.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            // Configure StockQuantity value object (Owned Entity Pattern)
            entity.OwnsOne(p => p.Stock, stock =>
            {
                stock.Property(s => s.Quantity)
                    .HasColumnName("Stock")
                    .IsRequired();
            });

            // Configure ProductRating value object (Owned Entity Pattern)
            entity.OwnsOne(p => p.Rating, rating =>
            {
                rating.Property(r => r.AverageRating)
                    .HasColumnName("AverageRating")
                    .HasPrecision(3, 2);

                rating.Property(r => r.TotalRatings)
                    .HasColumnName("TotalRatings");
            });

            // Ignore domain events (not persisted)
            entity.Ignore(p => p.DomainEvents);

            // Create indexes for performance
            entity.HasIndex(p => p.Category);
            entity.HasIndex(p => p.Name);
            entity.HasIndex(p => p.IsActive);
        });
    }

    /// <summary>
    /// Override SaveChanges to automatically set audit fields.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<Product>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // UpdatedAt is set via domain logic, but we ensure it's set
            if (entry.Property(nameof(Product.UpdatedAt)).CurrentValue == null)
            {
                entry.Property(nameof(Product.UpdatedAt)).CurrentValue = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
