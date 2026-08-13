using Microsoft.AspNetCore.Mvc;
using StockService.DTOs;
using StockService.Services;

namespace StockService.Controllers;

[ApiController]
[Route("api/stock")]
public sealed class StockController(StockManagementService stockService) : ControllerBase
{
    [HttpPost("deduct")]
    public async Task<ActionResult<StockDeductionResponse>> Deduct(DeductStockRequest request, CancellationToken cancellationToken) =>
        Ok(await stockService.DeductAsync(request, cancellationToken));
}
