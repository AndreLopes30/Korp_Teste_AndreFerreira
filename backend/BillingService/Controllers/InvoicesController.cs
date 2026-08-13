using BillingService.DTOs;
using BillingService.Services;
using Microsoft.AspNetCore.Mvc;

namespace BillingService.Controllers;

[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(InvoiceService invoiceService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<InvoiceDetailResponse>> Create(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByNumber), new { number = invoice.Number }, invoice);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceSummaryResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await invoiceService.ListAsync(cancellationToken));

    [HttpGet("{number:int}")]
    public async Task<ActionResult<InvoiceDetailResponse>> GetByNumber(int number, CancellationToken cancellationToken) =>
        Ok(await invoiceService.GetAsync(number, cancellationToken));

    [HttpPost("{number:int}/close")]
    public async Task<ActionResult<InvoiceDetailResponse>> Close(int number, CancellationToken cancellationToken) =>
        Ok(await invoiceService.CloseAsync(number, cancellationToken));
}
