using Microsoft.EntityFrameworkCore;
using OrderService.Api.Domain;

namespace OrderService.Api.Infrastructure;

/// <summary>
/// Order Service DbContext - EF Core
/// </summary>
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order entity configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(o => o.Id);

            entity.Property(o => o.CustomerId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(o => o.Status)
                .IsRequired()
                .HasConversion<string>(); // Store Enum as string

            entity.Property(o => o.CreatedAt)
                .IsRequired();

            // Order -> OrderItem relationship (1:N)
            entity.HasMany(o => o.Items)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore TotalAmount (computed property)
            entity.Ignore(o => o.TotalAmount);
        });

        // OrderItem entity configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(i => i.Id);

            entity.Property(i => i.ProductId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(i => i.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(i => i.Quantity)
                .IsRequired();

            entity.Property(i => i.UnitPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
        });
    }
}
