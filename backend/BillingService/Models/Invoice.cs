namespace BillingService.Models;

public sealed class Invoice
{
    public int Number { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public List<InvoiceItem> Items { get; set; } = [];
}
