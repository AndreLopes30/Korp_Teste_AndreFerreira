using BillingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Data;

public sealed class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var invoice = modelBuilder.Entity<Invoice>();
        invoice.ToTable("Invoices");
        invoice.HasKey(item => item.Number);
        invoice.Property(item => item.Number).ValueGeneratedOnAdd();
        invoice.Property(item => item.Status)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();
        invoice.Property(item => item.CreatedAtUtc).IsRequired();
        invoice.HasMany(item => item.Items)
            .WithOne(item => item.Invoice)
            .HasForeignKey(item => item.InvoiceNumber)
            .OnDelete(DeleteBehavior.Cascade);

        var invoiceItem = modelBuilder.Entity<InvoiceItem>();
        invoiceItem.ToTable("InvoiceItems", table =>
            table.HasCheckConstraint("CK_InvoiceItems_Quantity", "Quantity > 0"));
        invoiceItem.HasKey(item => item.Id);
        invoiceItem.Property(item => item.ProductCode).HasMaxLength(50).IsRequired();
        invoiceItem.Property(item => item.ProductDescription).HasMaxLength(200).IsRequired();
        invoiceItem.HasIndex(item => new { item.InvoiceNumber, item.ProductId })
            .IsUnique()
            .HasDatabaseName("UX_InvoiceItems_InvoiceNumber_ProductId");
    }
}
