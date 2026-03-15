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

    public async Task<InsulatedBoxInfo[]> GetInsulatedBoxesAsync(CancellationToken cancellationToken = default)
    {
        var boxes = await httpClient.GetFromJsonAsync<InsulatedBoxInfo[]>(
            "/products/insulated-boxes", JsonOptions, cancellationToken);
        return boxes ?? [];
    }

    public async Task<StandingOrderResult?> CreateStandingOrderAsync(
        Guid customerId, CreateStandingOrderRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/customers/{customerId}/standing-orders", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<StandingOrderResult>(JsonOptions, cancellationToken);
    }

    public async Task<OrderDetail?> CreateFedExOrderAsync(
        Guid customerId, CreateFedExOrderRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/customers/{customerId}/fedex-orders", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }
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
public enum InsulatedBoxSize { FiveLb = 5, TenLb = 10, TwelveLb = 12 }
public enum OrderFrequency { Weekly, BiWeekly, Monthly, OnRequest, Stopped }
public enum MonthlyWeek { First = 1, Second = 2, Third = 3, Fourth = 4 }

public record FedExTierInfo(string TierSize, decimal WeightOz, decimal FixedPrice);
public record InsulatedBoxInfo(string Size, decimal WeightLbs, decimal BasePricePerLb);
public record FedExOrderLineRequest(FedExTierSize TierSize, int Qty);
public record CreateFedExOrderRequest(Guid? ContactId, IReadOnlyList<FedExOrderLineRequest> Lines, DateTime? WeekOf = null, bool IsSample = false);
public record StandingOrderLineRequest(InsulatedBoxSize BoxSize, int Qty);
public record CreateStandingOrderRequest(Guid? ContactId, OrderFrequency Frequency, MonthlyWeek? MonthlyWeek, bool IsSample, IReadOnlyList<StandingOrderLineRequest> Lines);
public record StandingOrderResult(Guid Id);
public record UpdateOrderRequest(Guid? ContactId, bool IsSample);

public record OrderSummary(Guid Id, Guid CustomerId, string Channel, string Status, DateTime WeekOf, bool IsSample, int TotalQty, decimal TotalAmount);
public record OrderLine(Guid Id, string? BoxSize, int Qty, decimal? EffectivePricePerLb, string? FedExTierSize, decimal? FedExFixedPrice, string? PackagingType);
public record OrderDetail(Guid Id, Guid CustomerId, string Channel, string Status, DateTime WeekOf, bool IsSample, Guid? ContactId, int TotalQty, decimal TotalAmount, IReadOnlyList<OrderLine> Lines);
