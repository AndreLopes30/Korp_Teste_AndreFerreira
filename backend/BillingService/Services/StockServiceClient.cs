using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BillingService.DTOs;
using BillingService.Infrastructure;

namespace BillingService.Services;

public interface IStockServiceClient
{
    Task<IReadOnlyList<StockProductResponse>> GetProductsAsync(CancellationToken cancellationToken);
    Task DeductAsync(IReadOnlyList<StockDeductionItemRequest> items, CancellationToken cancellationToken);
}

public sealed class StockServiceClient(HttpClient httpClient) : IStockServiceClient
{
    public async Task<IReadOnlyList<StockProductResponse>> GetProductsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync("api/products", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw StockUnavailable();
            }

            return await response.Content.ReadFromJsonAsync<List<StockProductResponse>>(cancellationToken)
                ?? [];
        }
        catch (ApiException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw StockUnavailable();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw StockUnavailable();
        }
    }

    public async Task DeductAsync(
        IReadOnlyList<StockDeductionItemRequest> items,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/stock/deduct",
                new StockDeductionRequest(items),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            StockProblemResponse? problem = null;
            try
            {
                problem = await response.Content.ReadFromJsonAsync<StockProblemResponse>(cancellationToken);
            }
            catch (JsonException)
            {
                // An unreadable dependency response is handled as service unavailability below.
            }

            if (response.StatusCode == HttpStatusCode.Conflict && problem?.Code == "insufficient_stock")
            {
                throw new ApiException(
                    409,
                    "insufficient_stock",
                    "Estoque insuficiente",
                    problem.Detail ?? "Um ou mais produtos não possuem saldo suficiente.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound && problem?.Code == "product_not_found")
            {
                throw new ApiException(
                    409,
                    "stock_product_missing",
                    "Produto indisponível no estoque",
                    problem.Detail ?? "Um produto desta nota não existe mais no serviço de estoque.");
            }

            throw StockUnavailable();
        }
        catch (ApiException)
        {
            throw;
        }
        catch (HttpRequestException)
        {
            throw StockUnavailable();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw StockUnavailable();
        }
    }

    private static ApiException StockUnavailable() =>
        new(
            503,
            "stock_service_unavailable",
            "Serviço de estoque indisponível",
            "Não foi possível comunicar com o serviço de estoque. Tente novamente quando ele estiver disponível.");
}
