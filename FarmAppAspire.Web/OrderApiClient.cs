using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmAppAspire.Web;

public class OrderApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<OrderSummary[]> GetOrdersAsync(
        Guid customerId, string? status = null, CancellationToken cancellationToken = default)
    {
        var url = $"/customers/{customerId}/order-instances"
                  + (status is not null ? $"?status={status}" : "");
        var result = await httpClient.GetFromJsonAsync<IEnumerable<OrderSummary>>(url, JsonOptions, cancellationToken);
        return result?.ToArray() ?? [];
    }

    public async Task<OrderDetail?> GetOrderAsync(
        Guid customerId, Guid orderId, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<OrderDetail>(
            $"/customers/{customerId}/order-instances/{orderId}", JsonOptions, cancellationToken);

    public async Task<FedExTierInfo[]> GetFedExTiersAsync(CancellationToken cancellationToken = default)
    {
        var tiers = await httpClient.GetFromJsonAsync<FedExTierInfo[]>(
            "/products/fedex-tiers", JsonOptions, cancellationToken);
        return tiers ?? [];
    }

    public async Task<OrderDetail?> CreateFedExOrderAsync(
        Guid customerId, CreateFedExOrderRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/customers/{customerId}/fedex-orders", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<OrderDetail>(JsonOptions, cancellationToken);
    }

    public async Task<bool> CancelOrderAsync(
        Guid customerId, Guid instanceId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"/customers/{customerId}/order-instances/{instanceId}/cancel", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PatchOrderAsync(
        Guid customerId, Guid instanceId, UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PatchAsJsonAsync(
            $"/customers/{customerId}/order-instances/{instanceId}", request, JsonOptions, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}

public enum FedExTierSize { OneOz, TwoOz, FourOz, EightOz, OneLb, TwoLb, ThreeLb, FiveLb }

public record FedExTierInfo(string TierSize, decimal WeightOz, decimal FixedPrice);
public record FedExOrderLineRequest(FedExTierSize TierSize, int Qty);
public record CreateFedExOrderRequest(Guid? ContactId, IReadOnlyList<FedExOrderLineRequest> Lines);
public record UpdateOrderRequest(Guid? ContactId, bool IsSample);

public record OrderSummary(Guid Id, Guid CustomerId, string Channel, string Status, DateTime WeekOf, bool IsSample);
public record OrderDetail(Guid Id, Guid CustomerId, string Channel, string Status, DateTime WeekOf, bool IsSample, Guid? ContactId);
