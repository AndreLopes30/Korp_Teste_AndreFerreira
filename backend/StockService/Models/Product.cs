namespace StockService.Models;

public sealed class Product
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Description { get; set; }
    public int Balance { get; set; }
}
