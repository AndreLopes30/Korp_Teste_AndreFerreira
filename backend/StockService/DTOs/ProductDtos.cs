using System.ComponentModel.DataAnnotations;

namespace StockService.DTOs;

public sealed class CreateProductRequest
{
    [Required, StringLength(50)]
    public string Code { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Description { get; init; } = string.Empty;

    [Required, Range(0, int.MaxValue)]
    public int? Balance { get; init; }
}

public sealed record ProductResponse(int Id, string Code, string Description, int Balance);
