namespace BillingService.Models;

public sealed class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceNumber { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public int ProductId { get; set; }
    public required string ProductCode { get; set; }
    public required string ProductDescription { get; set; }
    public int Quantity { get; set; }
}
