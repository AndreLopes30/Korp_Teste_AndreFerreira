using System.ComponentModel.DataAnnotations;
using BillingService.Models;

namespace BillingService.DTOs;

public sealed class CreateInvoiceRequest
{
    [Required, MinLength(1)]
    public List<CreateInvoiceItemRequest> Items { get; init; } = [];
}

public sealed class CreateInvoiceItemRequest
{
    [Required, Range(1, int.MaxValue)]
    public int? ProductId { get; init; }

    [Required, Range(1, int.MaxValue)]
    public int? Quantity { get; init; }
}

public sealed record InvoiceItemResponse(
    int ProductId,
    string ProductCode,
    string ProductDescription,
    int Quantity);

public sealed record InvoiceSummaryResponse(
    int Number,
    InvoiceStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    int TotalQuantity);

public sealed record InvoiceDetailResponse(
    int Number,
    InvoiceStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    IReadOnlyList<InvoiceItemResponse> Items);
