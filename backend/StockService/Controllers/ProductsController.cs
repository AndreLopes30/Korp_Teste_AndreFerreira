using Microsoft.AspNetCore.Mvc;
using StockService.DTOs;
using StockService.Services;

namespace StockService.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(StockManagementService stockService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await stockService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await stockService.ListProductsAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await stockService.GetProductAsync(id, cancellationToken));
}
