using Microsoft.EntityFrameworkCore;
using CatalogService.Models;

namespace CatalogService.Data;

/// <summary>
/// DbContext для CatalogService.
/// Каждый микросервис владеет только своей БД — здесь только таблицы каталога.
/// </summary>
public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options)
    {
    }

    /// <summary>Таблица товаров.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>
    /// Настройка схемы БД: индексы, ограничения, seed-данные.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(p => p.Description)
                  .HasMaxLength(2000);

            entity.Property(p => p.Category)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(p => p.ImageUrl)
                  .HasMaxLength(500);

            // Точность для денег: 18 цифр, 2 после запятой
            entity.Property(p => p.Price)
                  .HasPrecision(18, 2);

            // Индекс для фильтрации по категории
            entity.HasIndex(p => p.Category);

            // Частичный индекс — только активные товары.
            // Ускоряет выборки каталога (редко бывает нужен список удалённых).
            entity.HasIndex(p => p.IsActive)
                  .HasFilter("\"IsActive\" = true");

            // Seed-данные для тестирования — 2 товара при первом запуске.
            // ВАЖНО: используем ФИКСИРОВАННЫЕ даты, а не DateTime.UtcNow —
            // иначе EF Core ругается на "PendingModelChangesWarning".
            entity.HasData(
                new Product
                {
                    Id = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                    Name = "iPhone 15 Pro",
                    Description = "Смартфон Apple с чипом A17 Pro",
                    Price = 89990.00m,
                    Stock = 15,
                    Category = "Смартфоны",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("8a7b6c5d-4e3f-2a1b-9c8d-7e6f5a4b3c2d"),
                    Name = "MacBook Air M3",
                    Description = "Ноутбук Apple с чипом M3",
                    Price = 129990.00m,
                    Stock = 8,
                    Category = "Ноутбуки",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });
    }
}