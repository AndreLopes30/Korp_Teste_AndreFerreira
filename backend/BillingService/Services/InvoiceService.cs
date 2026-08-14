using BillingService.Data;
using BillingService.DTOs;
using BillingService.Infrastructure;
using BillingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Services;

public sealed class InvoiceService(BillingDbContext dbContext, IStockServiceClient stockServiceClient)
{
    public async Task<InvoiceDetailResponse> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0 || request.Items.Any(item => item.ProductId is null or <= 0 || item.Quantity is null or <= 0))
        {
            throw new ApiException(400, "validation_error", "Dados inválidos", "Informe ao menos um produto com quantidade positiva.");
        }

        var items = request.Items
            .Select(item => new { ProductId = item.ProductId!.Value, Quantity = item.Quantity!.Value })
            .ToList();

        if (items.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
        {
            throw new ApiException(400, "validation_error", "Produtos duplicados", "Cada produto pode aparecer apenas uma vez na nota fiscal.");
        }

        var products = await stockServiceClient.GetProductsAsync(cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);
        var missingId = items.Select(item => item.ProductId).FirstOrDefault(id => !productsById.ContainsKey(id));
        if (missingId != 0)
        {
            throw new ApiException(400, "product_not_found", "Produto não encontrado", $"O produto {missingId} não está disponível no cadastro de estoque.");
        }

        var invoice = new Invoice
        {
            Status = InvoiceStatus.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Items = items.Select(item =>
            {
                var product = productsById[item.ProductId];
                return new InvoiceItem
                {
                    ProductId = product.Id,
                    ProductCode = product.Code,
                    ProductDescription = product.Description,
                    Quantity = item.Quantity
                };
            }).ToList()
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapDetail(invoice);
    }

    public async Task<IReadOnlyList<InvoiceSummaryResponse>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Invoices
            .AsNoTracking()
            .OrderByDescending(invoice => invoice.Number)
            .Select(invoice => new InvoiceSummaryResponse(
                invoice.Number,
                invoice.Status,
                invoice.CreatedAtUtc,
                invoice.ClosedAtUtc,
                invoice.Items.Sum(item => item.Quantity)))
            .ToListAsync(cancellationToken);

    public async Task<InvoiceDetailResponse> GetAsync(int number, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Number == number, cancellationToken);

        return invoice is null ? throw InvoiceNotFound(number) : MapDetail(invoice);
    }

    public async Task<InvoiceDetailResponse> CloseAsync(int number, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.Invoices
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Number == number, cancellationToken)
            ?? throw InvoiceNotFound(number);

        if (invoice.Status != InvoiceStatus.Open)
        {
            throw new ApiException(409, "invoice_not_open", "Nota fiscal já fechada", $"A nota fiscal {number} não está aberta para processamento.");
        }

        var deductionItems = invoice.Items
            .Select(item => new StockDeductionItemRequest(item.ProductId, item.Quantity))
            .ToList();
        await stockServiceClient.DeductAsync(deductionItems, cancellationToken);

        invoice.Status = InvoiceStatus.Closed;
        invoice.ClosedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapDetail(invoice);
    }

    private static InvoiceDetailResponse MapDetail(Invoice invoice) =>
        new(
            invoice.Number,
            invoice.Status,
            invoice.CreatedAtUtc,
            invoice.ClosedAtUtc,
            invoice.Items
                .OrderBy(item => item.Id)
                .Select(item => new InvoiceItemResponse(
                    item.ProductId,
                    item.ProductCode,
                    item.ProductDescription,
                    item.Quantity))
                .ToList());

    private static ApiException InvoiceNotFound(int number) =>
        new(404, "invoice_not_found", "Nota fiscal não encontrada", $"A nota fiscal {number} não foi encontrada.");
}
