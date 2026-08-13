using Microsoft.EntityFrameworkCore;
using StockService.Models;

namespace StockService.Data;

public sealed class StockDbContext(DbContextOptions<StockDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var product = modelBuilder.Entity<Product>();
        product.ToTable("Products", table =>
            table.HasCheckConstraint("CK_Products_Balance", "Balance >= 0"));
        product.HasKey(item => item.Id);
        product.Property(item => item.Code)
            .HasMaxLength(50)
            .UseCollation("NOCASE")
            .IsRequired();
        product.Property(item => item.Description)
            .HasMaxLength(200)
            .IsRequired();
        product.HasIndex(item => item.Code)
            .IsUnique()
            .HasDatabaseName("UX_Products_Code");
    }
}
