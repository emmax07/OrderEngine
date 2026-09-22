using Microsoft.EntityFrameworkCore;
using MassTransit;
using System.Runtime.InteropServices;

namespace OrderService.Data;

public class Order
{
    public Guid Id {get; set; }
    public string CustomerId {get; set; } = string.Empty;
    public decimal TotalAmount {get; set; }
    public string Status {get; set; } = "Pending";
    public DateTime CreatedAt {get; set; }
}

public class OrderDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MassTransit Entity Framework Outbox tables configuration
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
        });
    }
}