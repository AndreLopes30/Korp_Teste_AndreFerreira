using System.ComponentModel.DataAnnotations;

namespace StockService.DTOs;

public sealed class DeductStockRequest
{
    [Required, MinLength(1)]
    public List<DeductStockItemRequest> Items { get; init; } = [];
}

public sealed class DeductStockItemRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? ProductId { get; init; }

    [Required, Range(1, int.MaxValue)]
    public int? Quantity { get; init; }
}

public sealed record StockDeductionItemResponse(
    int ProductId,
    int PreviousBalance,
    int DeductedQuantity,
    int FinalBalance);

public sealed record StockDeductionResponse(IReadOnlyList<StockDeductionItemResponse> Items);
