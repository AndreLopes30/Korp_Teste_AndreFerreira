using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StockService.Data;
using StockService.DTOs;
using StockService.Infrastructure;
using StockService.Models;

namespace StockService.Services;

public sealed class StockManagementService(StockDbContext dbContext)
{
    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        var description = request.Description.Trim();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(description) || request.Balance is null)
        {
            throw new ApiException(400, "validation_error", "Dados inválidos", "Código, descrição e saldo são obrigatórios.");
        }

        if (await dbContext.Products.AnyAsync(item => item.Code == code, cancellationToken))
        {
            throw DuplicateCode(code);
        }

        var product = new Product { Code = code, Description = description, Balance = request.Balance.Value };
        dbContext.Products.Add(product);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            throw DuplicateCode(code);
        }

        return ToResponse(product);
    }

    public async Task<IReadOnlyList<ProductResponse>> ListProductsAsync(CancellationToken cancellationToken) =>
        await dbContext.Products
            .AsNoTracking()
            .OrderBy(item => item.Code)
            .Select(item => new ProductResponse(item.Id, item.Code, item.Description, item.Balance))
            .ToListAsync(cancellationToken);

    public async Task<ProductResponse> GetProductAsync(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new ProductResponse(item.Id, item.Code, item.Description, item.Balance))
            .SingleOrDefaultAsync(cancellationToken);

        return product ?? throw new ApiException(404, "product_not_found", "Produto não encontrado", $"O produto {id} não foi encontrado.");
    }

    public async Task<StockDeductionResponse> DeductAsync(DeductStockRequest request, CancellationToken cancellationToken)
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
            throw new ApiException(400, "validation_error", "Produtos duplicados", "Cada produto deve aparecer apenas uma vez na dedução.");
        }

        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);

        var missingId = productIds.FirstOrDefault(id => !productsById.ContainsKey(id));
        if (missingId != 0)
        {
            throw new ApiException(404, "product_not_found", "Produto não encontrado", $"O produto {missingId} não foi encontrado.");
        }

        var insufficientItem = items.FirstOrDefault(item => productsById[item.ProductId].Balance < item.Quantity);
        if (insufficientItem is not null)
        {
            var product = productsById[insufficientItem.ProductId];
            throw new ApiException(
                409,
                "insufficient_stock",
                "Estoque insuficiente",
                $"O produto {product.Code} possui saldo {product.Balance}, menor que a quantidade solicitada {insufficientItem.Quantity}.");
        }

        var result = items.Select(item =>
        {
            var product = productsById[item.ProductId];
            var previousBalance = product.Balance;
            product.Balance -= item.Quantity;
            return new StockDeductionItemResponse(product.Id, previousBalance, item.Quantity, product.Balance);
        }).ToList();

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new StockDeductionResponse(result);
    }

    private static ProductResponse ToResponse(Product product) =>
        new(product.Id, product.Code, product.Description, product.Balance);

    private static ApiException DuplicateCode(string code) =>
        new(409, "duplicate_product_code", "Código já cadastrado", $"Já existe um produto cadastrado com o código {code}.");
}
