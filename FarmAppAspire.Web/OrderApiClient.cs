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

    // ── Standing order management ─────────────────────────────────────────────

    public async Task<AllStandingOrderSummary[]> GetAllStandingOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<IEnumerable<AllStandingOrderSummary>>(
            "/standing-orders", JsonOptions, cancellationToken);
        return result?.ToArray() ?? [];
    }

    public async Task<StandingOrderDetail[]> GetStandingOrdersAsync(
        Guid customerId, CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<IEnumerable<StandingOrderDetail>>(
            $"/customers/{customerId}/standing-orders", JsonOptions, cancellationToken);
        return result?.ToArray() ?? [];
    }

    public async Task<StandingOrderDetail?> GetStandingOrderAsync(
        Guid customerId, Guid soId, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<StandingOrderDetail>(
            $"/customers/{customerId}/standing-orders/{soId}", JsonOptions, cancellationToken);

    public async Task<StandingOrderDetail?> UpdateStandingOrderAsync(
        Guid customerId, Guid soId, WebUpdateStandingOrderRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"/customers/{customerId}/standing-orders/{soId}", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<StandingOrderDetail>(JsonOptions, cancellationToken);
    }

    public async Task<bool> PauseStandingOrderAsync(
        Guid customerId, Guid soId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"/customers/{customerId}/standing-orders/{soId}/pause", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ResumeStandingOrderAsync(
        Guid customerId, Guid soId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"/customers/{customerId}/standing-orders/{soId}/resume", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> StopStandingOrderAsync(
        Guid customerId, Guid soId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"/customers/{customerId}/standing-orders/{soId}/stop", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddSkipWeekAsync(
        Guid customerId, Guid soId, DateTime weekOf, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/customers/{customerId}/standing-orders/{soId}/skip",
            new AddSkipWeekRequest(weekOf), JsonOptions, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    // ── Order instance lifecycle ──────────────────────────────────────────────

    public async Task<OrderDetail?> HarvestOrderAsync(
        Guid customerId, Guid instanceId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"/customers/{customerId}/order-instances/{instanceId}/harvest", null, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<OrderDetail>(JsonOptions, cancellationToken);
    }

    public async Task<OrderDetail?> InspectOrderAsync(
        Guid customerId, Guid instanceId, DateTime inspectionDate, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/customers/{customerId}/order-instances/{instanceId}/inspect",
            new InspectOrderRequest(inspectionDate), JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<OrderDetail>(JsonOptions, cancellationToken);
    }

    public async Task<OrderDetail?> ShipOrderAsync(
        Guid customerId, Guid instanceId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync(
            $"/customers/{customerId}/order-instances/{instanceId}/ship", null, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<OrderDetail>(JsonOptions, cancellationToken);
    }

    // ── Admin: instance generation ────────────────────────────────────────────

    public async Task<GenerateInstancesResponse?> GenerateInstancesAsync(
        DateTime forWeek, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/admin/generate-instances",
            new GenerateInstancesRequest(forWeek),
            JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<GenerateInstancesResponse>(JsonOptions, cancellationToken);
    }

    public async Task<WeekInstanceSummary[]> BrowseWeekInstancesAsync(
        DateTime weekOf, CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<IEnumerable<WeekInstanceSummary>>(
            $"/admin/instances?weekOf={weekOf:yyyy-MM-dd}", JsonOptions, cancellationToken);
        return result?.ToArray() ?? [];
    }
}

public enum FedExTierSize { OneOz, TwoOz, FourOz, EightOz, OneLb, TwoLb, ThreeLb, FiveLb }
public enum InsulatedBoxSize { FiveLb = 5, TenLb = 10, TwelveLb = 12 }
public enum OrderFrequency { Weekly, BiWeekly, Monthly, OnRequest, Stopped }
public enum MonthlyWeek { First = 1, Second = 2, Third = 3, Fourth = 4 }

public record FedExTierInfo(string TierSize, decimal WeightOz, decimal FixedPrice);
public record InsulatedBoxInfo(string Size, decimal WeightLbs, decimal BasePricePerLb, bool IsDefault = false);
public record FedExOrderLineRequest(FedExTierSize TierSize, int Qty);
public record CreateFedExOrderRequest(Guid? ContactId, IReadOnlyList<FedExOrderLineRequest> Lines, DateTime? WeekOf = null, bool IsSample = false);
public record StandingOrderLineRequest(InsulatedBoxSize BoxSize, int Qty);
public record CreateStandingOrderRequest(Guid? ContactId, OrderFrequency Frequency, MonthlyWeek? MonthlyWeek, bool IsSample, IReadOnlyList<StandingOrderLineRequest> Lines);
public record StandingOrderResult(Guid Id);
public record UpdateOrderRequest(Guid? ContactId, bool IsSample);
public record WebUpdateStandingOrderRequest(OrderFrequency Frequency, MonthlyWeek? MonthlyWeek, bool IsSample, Guid? ContactId, IReadOnlyList<StandingOrderLineRequest> Lines);
public record AddSkipWeekRequest(DateTime WeekOf);
public record InspectOrderRequest(DateTime InspectionDate);

public record StandingOrderLineDetail(Guid Id, string BoxSize, int Qty);
public record StandingOrderSkipDetail(Guid Id, DateTime WeekOf);
public record AllStandingOrderSummary(
    Guid Id, Guid CustomerId, string CustomerDisplayName,
    ChannelType CustomerChannelType, CustomerType CustomerType,
    string Status, string Frequency, string? MonthlyWeek,
    bool IsSample, int SeasonYear, DateTime StartWeek,
    int TotalBoxes, decimal TotalWeightLbs, decimal TotalAmount,
    DateTime CreatedAt, string CreatedBy);
public record StandingOrderDetail(
    Guid Id, Guid CustomerId, Guid? ContactId,
    string Status, string Frequency, string? MonthlyWeek,
    bool IsSample, int SeasonYear, DateTime StartWeek,
    IReadOnlyList<StandingOrderLineDetail> Lines,
    IReadOnlyList<StandingOrderSkipDetail> Skips,
    int TotalBoxes, decimal TotalWeightLbs, decimal TotalAmount,
    DateTime CreatedAt, string CreatedBy);

public record OrderSummary(Guid Id, Guid? StandingOrderId, Guid CustomerId, string Channel, string Status, DateTime WeekOf, bool IsSample, int TotalQty, decimal TotalAmount);
public record OrderLine(Guid Id, string? BoxSize, int Qty, decimal? EffectivePricePerLb, string? FedExTierSize, decimal? FedExFixedPrice, string? PackagingType);
public record OrderDetail(Guid Id, Guid? StandingOrderId, Guid CustomerId, string Channel, string Status, DateTime WeekOf, bool IsSample, Guid? ContactId, int TotalQty, decimal TotalAmount, IReadOnlyList<OrderLine> Lines);

// ── Admin DTOs ────────────────────────────────────────────────────────────────
public record GenerateInstancesRequest(DateTime ForWeek);
public record GeneratedInstanceSummary(Guid InstanceId, Guid CustomerId, string CustomerDisplayName, string? CustomerKey, string Channel, DateTime WeekOf, bool IsSample, int TotalQty, decimal TotalAmount);
public record GenerateInstancesResponse(int GeneratedCount, int SkippedCount, DateTime WeekOf, IReadOnlyList<GeneratedInstanceSummary> Instances);
public record WeekInstanceSummary(Guid InstanceId, Guid CustomerId, string CustomerDisplayName, string? CustomerKey, string Channel, string Status, DateTime WeekOf, bool IsSample, int TotalQty, decimal TotalAmount);
