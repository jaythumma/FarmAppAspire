using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FarmAppAspire.Tests.Unit.Helpers;
using FarmAppAspire.Web;

namespace FarmAppAspire.Tests.Unit.Web;

/// <summary>
/// Validates that <see cref="OrderApiClient"/> routes requests to the correct
/// API endpoint based on customer channel type (Issue #26).
/// </summary>
public class OrderChannelRoutingTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static (OrderApiClient client, Func<HttpRequestMessage?> getLastRequest) BuildClient(
        HttpResponseMessage response)
    {
        HttpRequestMessage? captured = null;
        var stub = new DelegatingHandlerStub((req, _) =>
        {
            captured = req;
            return Task.FromResult(response);
        });

        var http = new HttpClient(stub) { BaseAddress = new Uri("https://customerservice") };
        return (new OrderApiClient(http), () => captured);
    }

    // ── Direct channel → standing order endpoint ──────────────────────────────

    [Fact]
    public async Task CreateStandingOrderAsync_PostsToStandingOrdersEndpoint()
    {
        var customerId = Guid.NewGuid();
        var responseBody = JsonSerializer.Serialize(new { Id = Guid.NewGuid() }, JsonOptions);
        var (client, getRequest) = BuildClient(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
        });

        var req = new CreateStandingOrderRequest(
            ContactId: null,
            Frequency: OrderFrequency.Weekly,
            MonthlyWeek: null,
            IsSample: false,
            Lines: [new StandingOrderLineRequest(InsulatedBoxSize.TenLb, 2)]);

        var result = await client.CreateStandingOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        var request = getRequest();
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains($"/customers/{customerId}/standing-orders", request.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateStandingOrderAsync_SerialisesLinesWithInsulatedBoxSize()
    {
        var customerId = Guid.NewGuid();
        string? capturedBody = null;
        var stub = new DelegatingHandlerStub(async (req, _) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { Id = Guid.NewGuid() }, JsonOptions),
                    System.Text.Encoding.UTF8, "application/json")
            };
        });

        var http = new HttpClient(stub) { BaseAddress = new Uri("https://customerservice") };
        var client = new OrderApiClient(http);

        var req = new CreateStandingOrderRequest(
            ContactId: null,
            Frequency: OrderFrequency.Monthly,
            MonthlyWeek: MonthlyWeek.Second,
            IsSample: false,
            Lines: [new StandingOrderLineRequest(InsulatedBoxSize.FiveLb, 3)]);

        await client.CreateStandingOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        Assert.NotNull(capturedBody);
        Assert.Contains("FiveLb", capturedBody);
        Assert.Contains("Monthly", capturedBody);
        Assert.Contains("Second", capturedBody);
    }

    [Fact]
    public async Task CreateStandingOrderAsync_ThrowsHttpRequestException_OnErrorResponse()
    {
        var (client, _) = BuildClient(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("Customer already has an active standing order.")
        });

        var req = new CreateStandingOrderRequest(
            ContactId: null,
            Frequency: OrderFrequency.Weekly,
            MonthlyWeek: null,
            IsSample: false,
            Lines: [new StandingOrderLineRequest(InsulatedBoxSize.TenLb, 1)]);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.CreateStandingOrderAsync(Guid.NewGuid(), req, TestContext.Current.CancellationToken));
    }

    // ── Amazon channel → FedEx order endpoint ────────────────────────────────

    [Fact]
    public async Task CreateFedExOrderAsync_PostsToFedExOrdersEndpoint()
    {
        var customerId = Guid.NewGuid();
        var responseBody = JsonSerializer.Serialize(
            new { Id = Guid.NewGuid(), CustomerId = customerId, Channel = "FedEx",
                  Status = "Pending", WeekOf = DateTime.UtcNow, IsSample = false,
                  ContactId = (Guid?)null },
            JsonOptions);

        var (client, getRequest) = BuildClient(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
        });

        var req = new CreateFedExOrderRequest(
            ContactId: null,
            Lines: [new FedExOrderLineRequest(FedExTierSize.TwoLb, 1)],
            WeekOf: DateTime.UtcNow,
            IsSample: false);

        var result = await client.CreateFedExOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        var request = getRequest();
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains($"/customers/{customerId}/fedex-orders", request.RequestUri!.PathAndQuery);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateFedExOrderAsync_SerialisesLinesWithFedExTierSize()
    {
        var customerId = Guid.NewGuid();
        string? capturedBody = null;
        var stub = new DelegatingHandlerStub(async (req, _) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(
                        new { Id = Guid.NewGuid(), CustomerId = customerId, Channel = "FedEx",
                              Status = "Pending", WeekOf = DateTime.UtcNow, IsSample = false,
                              ContactId = (Guid?)null },
                        JsonOptions),
                    System.Text.Encoding.UTF8, "application/json")
            };
        });

        var http = new HttpClient(stub) { BaseAddress = new Uri("https://customerservice") };
        var client = new OrderApiClient(http);

        var req = new CreateFedExOrderRequest(
            ContactId: null,
            Lines: [new FedExOrderLineRequest(FedExTierSize.FiveLb, 2)],
            WeekOf: null,
            IsSample: false);

        await client.CreateFedExOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        Assert.NotNull(capturedBody);
        Assert.Contains("FiveLb", capturedBody);
    }

    // ── Insulated box catalogue ───────────────────────────────────────────────

    [Fact]
    public async Task GetInsulatedBoxesAsync_ReturnsBoxConfigs()
    {
        var boxes = new[]
        {
            new { Size = "FiveLb",   WeightLbs = 5m,  BasePricePerLb = 13m },
            new { Size = "TenLb",    WeightLbs = 10m, BasePricePerLb = 13m },
            new { Size = "TwelveLb", WeightLbs = 12m, BasePricePerLb = 13m }
        };

        var (client, getRequest) = BuildClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(boxes, JsonOptions),
                System.Text.Encoding.UTF8, "application/json")
        });

        var result = await client.GetInsulatedBoxesAsync(TestContext.Current.CancellationToken);

        var request = getRequest();
        Assert.NotNull(request);
        Assert.Contains("/products/insulated-boxes", request.RequestUri!.PathAndQuery);
        Assert.Equal(3, result.Length);
        Assert.Contains(result, b => b.Size == "FiveLb");
    }

    // ── GetOrdersAsync – totals deserialization ───────────────────────────────

    [Fact]
    public async Task GetOrdersAsync_DeserializesTotalQtyAndTotalAmount()
    {
        var customerId = Guid.NewGuid();
        var orders = new[]
        {
            new
            {
                Id         = Guid.NewGuid(),
                CustomerId = customerId,
                Channel    = "Direct",
                Status     = "Pending",
                WeekOf     = DateTime.UtcNow,
                IsSample   = false,
                TotalQty   = 5,
                TotalAmount = 325.00m
            },
            new
            {
                Id         = Guid.NewGuid(),
                CustomerId = customerId,
                Channel    = "FedEx",
                Status     = "Shipped",
                WeekOf     = DateTime.UtcNow.AddDays(-7),
                IsSample   = false,
                TotalQty   = 2,
                TotalAmount = 90.00m
            }
        };

        var (client, getRequest) = BuildClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(orders, JsonOptions),
                System.Text.Encoding.UTF8, "application/json")
        });

        var result = await client.GetOrdersAsync(customerId, cancellationToken: TestContext.Current.CancellationToken);

        var request = getRequest();
        Assert.NotNull(request);
        Assert.Contains($"/customers/{customerId}/order-instances", request.RequestUri!.PathAndQuery);
        Assert.Equal(2, result.Length);
        Assert.Equal(5,      result[0].TotalQty);
        Assert.Equal(325.00m, result[0].TotalAmount);
        Assert.Equal(2,      result[1].TotalQty);
        Assert.Equal(90.00m, result[1].TotalAmount);
    }

    [Fact]
    public async Task GetOrdersAsync_WithStatusFilter_AppendsStatusQueryParam()
    {
        var customerId = Guid.NewGuid();

        var (client, getRequest) = BuildClient(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
        });

        await client.GetOrdersAsync(customerId, status: "Pending", cancellationToken: TestContext.Current.CancellationToken);

        var request = getRequest();
        Assert.NotNull(request);
        Assert.Contains("status=Pending", request.RequestUri!.Query);
    }
}
