using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

/// <summary>
/// DbContext для OrderService.
/// Владеет только своими таблицами: заказы и позиции.
/// </summary>
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.Property(o => o.UserEmail)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(o => o.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.Property(o => o.TotalAmount)
                  .HasPrecision(18, 2);

            entity.Property(o => o.Comment)
                  .HasMaxLength(1000);

            entity.Property(o => o.ShippingStreet).HasMaxLength(200);
            entity.Property(o => o.ShippingCity).HasMaxLength(100);
            entity.Property(o => o.ShippingZipCode).HasMaxLength(20);

            // Составной индекс: заказы пользователя по дате убывания
            entity.HasIndex(o => new { o.UserId, o.CreatedAt })
                  .IsDescending(false, true);

            entity.HasIndex(o => o.Status);

            entity.HasMany(o => o.Items)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);

            entity.Property(i => i.ProductName)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(i => i.PriceAtPurchase)
                  .HasPrecision(18, 2);

            entity.HasIndex(i => i.OrderId);
        });
    }
}