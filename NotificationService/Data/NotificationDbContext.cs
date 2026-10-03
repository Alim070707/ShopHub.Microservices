using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data;

/// <summary>
/// DbContext для NotificationService.
/// Хранит журнал отправленных уведомлений.
/// </summary>
public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.UserEmail).IsRequired().HasMaxLength(255);
            entity.Property(l => l.Type).IsRequired().HasMaxLength(50);
            entity.Property(l => l.Subject).IsRequired().HasMaxLength(500);
            entity.Property(l => l.Body).IsRequired();
            entity.Property(l => l.Status).IsRequired().HasMaxLength(20);
            entity.Property(l => l.Error).HasMaxLength(2000);

            // Идемпотентность: одно событие — одна запись
            entity.HasIndex(l => l.EventId).IsUnique();

            // Ускоряет выборку уведомлений пользователя
            entity.HasIndex(l => new { l.UserId, l.CreatedAt });
        });
    }
}