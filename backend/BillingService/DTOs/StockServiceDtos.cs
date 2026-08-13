namespace BillingService.DTOs;

public sealed record StockProductResponse(int Id, string Code, string Description, int Balance);

public sealed record StockDeductionItemRequest(int ProductId, int Quantity);

public sealed record StockDeductionRequest(IReadOnlyList<StockDeductionItemRequest> Items);

public sealed record StockProblemResponse(string? Code, string? Detail);
