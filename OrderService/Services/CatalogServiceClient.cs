namespace OrderService.Services;

/// <summary>DTO — ответ от CatalogService о доступности товара.</summary>
public record ProductAvailability(
    Guid ProductId,
    string ProductName,
    int RequestedQuantity,
    bool InStock,
    decimal CurrentPrice);

/// <summary>
/// HTTP-клиент для взаимодействия с CatalogService.
/// Polly-политики (Retry + Circuit Breaker) настраиваются в Program.cs.
/// </summary>
public interface ICatalogServiceClient
{
    Task<ProductAvailability?> CheckAvailabilityAsync(Guid productId, int quantity, CancellationToken ct = default);
}

public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;

    public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Проверить наличие товара и получить текущую цену.
    /// Возвращает null, если товар не найден (404).
    /// Бросает HttpRequestException, если CatalogService недоступен.
    /// </summary>
    public async Task<ProductAvailability?> CheckAvailabilityAsync(
        Guid productId, int quantity, CancellationToken ct = default)
    {
        _logger.LogDebug("Проверка доступности товара {ProductId} (кол-во: {Qty})", productId, quantity);

        var response = await _httpClient.GetAsync(
            $"api/products/{productId}/availability?quantity={quantity}", ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductAvailability>(cancellationToken: ct);
    }
}