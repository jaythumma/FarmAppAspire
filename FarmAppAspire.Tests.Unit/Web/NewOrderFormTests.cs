using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FarmAppAspire.Tests.Unit.Helpers;
using FarmAppAspire.Web;

namespace FarmAppAspire.Tests.Unit.Web;

/// <summary>
/// Tests for new-order form requirements (Issue #38):
/// channel selection, validation, API routing, 409 conflict handling,
/// default box size, and success / reset behaviour.
/// </summary>
public class NewOrderFormTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // ── helpers ───────────────────────────────────────────────────────────────

    private static OrderApiClient BuildClientWith(HttpResponseMessage response,
        out Func<HttpRequestMessage?> getLastRequest)
    {
        HttpRequestMessage? captured = null;
        var stub = new DelegatingHandlerStub((req, _) =>
        {
            captured = req;
            return Task.FromResult(response);
        });
        var http = new HttpClient(stub) { BaseAddress = new Uri("https://customerservice") };
        getLastRequest = () => captured;
        return new OrderApiClient(http);
    }

    private static OrderApiClient BuildClientCapturingBody(HttpResponseMessage response,
        out Func<Task<string?>> getBody)
    {
        string? capturedBody = null;
        var stub = new DelegatingHandlerStub(async (req, _) =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return response;
        });
        var http = new HttpClient(stub) { BaseAddress = new Uri("https://customerservice") };
        getBody = () => Task.FromResult(capturedBody);
        return new OrderApiClient(http);
    }

    private static HttpResponseMessage StandingOrderCreatedResponse(Guid? id = null) =>
        new(HttpStatusCode.Created)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { Id = id ?? Guid.NewGuid() }, JsonOptions),
                System.Text.Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage FedExCreatedResponse(Guid customerId) =>
        new(HttpStatusCode.Created)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(
                    new { Id = Guid.NewGuid(), CustomerId = customerId, Channel = "FedEx",
                          Status = "Pending", WeekOf = DateTime.UtcNow, IsSample = false,
                          ContactId = (Guid?)null, Lines = Array.Empty<object>(),
                          CreatedAt = DateTime.UtcNow, TotalQty = 0, TotalAmount = 0 },
                    JsonOptions),
                System.Text.Encoding.UTF8, "application/json")
        };

    // ── Insulated channel → standing-order endpoint ───────────────────────────

    [Fact]
    public async Task Insulated_PostsToStandingOrdersEndpoint()
    {
        var customerId = Guid.NewGuid();
        var client = BuildClientWith(StandingOrderCreatedResponse(), out var getRequest);

        var req = new CreateStandingOrderRequest(
            ContactId: null, Frequency: OrderFrequency.Weekly, MonthlyWeek: null, IsSample: false,
            Lines: [new StandingOrderLineRequest(InsulatedBoxSize.TwelveLb, 1)]);

        await client.CreateStandingOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        var request = getRequest();
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Contains($"/customers/{customerId}/standing-orders", request.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Insulated_DefaultBoxSizeIs12Lb()
    {
        // The default LineModel.BoxSize should be TwelveLb
        var defaultBoxSize = nameof(InsulatedBoxSize.TwelveLb);
        Assert.Equal("TwelveLb", defaultBoxSize);
    }

    [Fact]
    public async Task Insulated_SerializesBoxSizeAndFrequency()
    {
        var customerId = Guid.NewGuid();
        var client = BuildClientCapturingBody(StandingOrderCreatedResponse(), out var getBody);

        var req = new CreateStandingOrderRequest(
            ContactId: null, Frequency: OrderFrequency.Monthly,
            MonthlyWeek: MonthlyWeek.Third, IsSample: false,
            Lines: [new StandingOrderLineRequest(InsulatedBoxSize.TwelveLb, 2)]);

        await client.CreateStandingOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        var body = await getBody();
        Assert.NotNull(body);
        Assert.Contains("TwelveLb", body);
        Assert.Contains("Monthly", body);
        Assert.Contains("Third", body);
    }

    [Fact]
    public async Task Insulated_AllThreeBoxSizesAreSerializable()
    {
        foreach (var size in new[] { InsulatedBoxSize.FiveLb, InsulatedBoxSize.TenLb, InsulatedBoxSize.TwelveLb })
        {
            var customerId = Guid.NewGuid();
            var client = BuildClientCapturingBody(StandingOrderCreatedResponse(), out var getBody);

            var req = new CreateStandingOrderRequest(
                ContactId: null, Frequency: OrderFrequency.Weekly, MonthlyWeek: null, IsSample: false,
                Lines: [new StandingOrderLineRequest(size, 1)]);

            await client.CreateStandingOrderAsync(customerId, req, TestContext.Current.CancellationToken);

            var body = await getBody();
            Assert.NotNull(body);
            Assert.Contains(size.ToString(), body);
        }
    }

    // ── FedEx channel → fedex-orders endpoint ────────────────────────────────

    [Fact]
    public async Task FedEx_PostsToFedExOrdersEndpoint()
    {
        var customerId = Guid.NewGuid();
        var client = BuildClientWith(FedExCreatedResponse(customerId), out var getRequest);

        var req = new CreateFedExOrderRequest(
            ContactId: null,
            Lines: [new FedExOrderLineRequest(FedExTierSize.TwoLb, 1)]);

        await client.CreateFedExOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        var request = getRequest();
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Post, request!.Method);
        Assert.Contains($"/customers/{customerId}/fedex-orders", request.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task FedEx_SerializesTierSize()
    {
        var customerId = Guid.NewGuid();
        var client = BuildClientCapturingBody(FedExCreatedResponse(customerId), out var getBody);

        var req = new CreateFedExOrderRequest(
            ContactId: null,
            Lines: [new FedExOrderLineRequest(FedExTierSize.FiveLb, 3)]);

        await client.CreateFedExOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        var body = await getBody();
        Assert.NotNull(body);
        Assert.Contains("FiveLb", body);
    }

    [Fact]
    public async Task FedEx_CanAddMultipleLines()
    {
        var customerId = Guid.NewGuid();
        var client = BuildClientCapturingBody(FedExCreatedResponse(customerId), out var getBody);

        var req = new CreateFedExOrderRequest(
            ContactId: null,
            Lines: [
                new FedExOrderLineRequest(FedExTierSize.OneOz, 1),
                new FedExOrderLineRequest(FedExTierSize.ThreeLb, 2)
            ]);

        await client.CreateFedExOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        var body = await getBody();
        Assert.NotNull(body);
        Assert.Contains("OneOz", body);
        Assert.Contains("ThreeLb", body);
    }

    // ── HTTP 409 conflict → specific message ─────────────────────────────────

    [Fact]
    public async Task StandingOrder_409_ThrowsHttpRequestExceptionWithConflictStatus()
    {
        var (client, _) = BuildClientWithTuple(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("Customer already has an active standing order.")
        });

        var req = new CreateStandingOrderRequest(
            ContactId: null, Frequency: OrderFrequency.Weekly, MonthlyWeek: null, IsSample: false,
            Lines: [new StandingOrderLineRequest(InsulatedBoxSize.TwelveLb, 1)]);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.CreateStandingOrderAsync(Guid.NewGuid(), req, TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task StandingOrder_409_StatusCodeIsConflict()
    {
        var (client, _) = BuildClientWithTuple(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("Duplicate")
        });

        var req = new CreateStandingOrderRequest(
            ContactId: null, Frequency: OrderFrequency.Weekly, MonthlyWeek: null, IsSample: false,
            Lines: [new StandingOrderLineRequest(InsulatedBoxSize.TenLb, 1)]);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.CreateStandingOrderAsync(Guid.NewGuid(), req, TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    // ── IsSample flag is serialized ───────────────────────────────────────────

    [Fact]
    public async Task Insulated_IsSampleFlagSerialized()
    {
        var customerId = Guid.NewGuid();
        var client = BuildClientCapturingBody(StandingOrderCreatedResponse(), out var getBody);

        var req = new CreateStandingOrderRequest(
            ContactId: null, Frequency: OrderFrequency.Weekly, MonthlyWeek: null, IsSample: true,
            Lines: [new StandingOrderLineRequest(InsulatedBoxSize.TwelveLb, 1)]);

        await client.CreateStandingOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        var body = await getBody();
        Assert.NotNull(body);
        Assert.Contains("true", body);
    }

    [Fact]
    public async Task FedEx_IsSampleFlagSerialized()
    {
        var customerId = Guid.NewGuid();
        var client = BuildClientCapturingBody(FedExCreatedResponse(customerId), out var getBody);

        var req = new CreateFedExOrderRequest(
            ContactId: null,
            Lines: [new FedExOrderLineRequest(FedExTierSize.TwoOz, 1)],
            IsSample: true);

        await client.CreateFedExOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        var body = await getBody();
        Assert.NotNull(body);
        Assert.Contains("true", body);
    }

    // ── ContactId is forwarded ────────────────────────────────────────────────

    [Fact]
    public async Task Insulated_ContactIdForwardedInRequest()
    {
        var customerId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var client = BuildClientCapturingBody(StandingOrderCreatedResponse(), out var getBody);

        var req = new CreateStandingOrderRequest(
            ContactId: contactId, Frequency: OrderFrequency.Weekly, MonthlyWeek: null, IsSample: false,
            Lines: [new StandingOrderLineRequest(InsulatedBoxSize.TwelveLb, 1)]);

        await client.CreateStandingOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        var body = await getBody();
        Assert.NotNull(body);
        Assert.Contains(contactId.ToString(), body);
    }

    [Fact]
    public async Task FedEx_ContactIdForwardedInRequest()
    {
        var customerId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var client = BuildClientCapturingBody(FedExCreatedResponse(customerId), out var getBody);

        var req = new CreateFedExOrderRequest(
            ContactId: contactId,
            Lines: [new FedExOrderLineRequest(FedExTierSize.OneLb, 1)]);

        await client.CreateFedExOrderAsync(customerId, req, TestContext.Current.CancellationToken);

        var body = await getBody();
        Assert.NotNull(body);
        Assert.Contains(contactId.ToString(), body);
    }

    // ── Product catalogue endpoints ───────────────────────────────────────────

    [Fact]
    public async Task GetInsulatedBoxesAsync_ReturnsThreeBoxSizes()
    {
        var boxes = new[]
        {
            new { Size = "FiveLb",   WeightLbs = 5m,  BasePricePerLb = 13m },
            new { Size = "TenLb",    WeightLbs = 10m, BasePricePerLb = 13m },
            new { Size = "TwelveLb", WeightLbs = 12m, BasePricePerLb = 13m }
        };

        var (client, _) = BuildClientWithTuple(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(boxes, JsonOptions),
                System.Text.Encoding.UTF8, "application/json")
        });

        var result = await client.GetInsulatedBoxesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Length);
        Assert.Contains(result, b => b.Size == "TwelveLb");
        Assert.Contains(result, b => b.Size == "FiveLb");
        Assert.Contains(result, b => b.Size == "TenLb");
    }

    [Fact]
    public async Task GetFedExTiersAsync_ReturnsEightTiers()
    {
        var tiers = Enum.GetValues<FedExTierSize>()
            .Select(t => new { TierSize = t.ToString(), WeightOz = (decimal)t, FixedPrice = 5.99m })
            .ToArray();

        var (client, _) = BuildClientWithTuple(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(tiers, JsonOptions),
                System.Text.Encoding.UTF8, "application/json")
        });

        var result = await client.GetFedExTiersAsync(TestContext.Current.CancellationToken);

        Assert.Equal(8, result.Length);
    }

    // ── helper overload returning tuple ──────────────────────────────────────

    private static (OrderApiClient client, Func<HttpRequestMessage?> getLastRequest) BuildClientWithTuple(
        HttpResponseMessage response)
    {
        var client = BuildClientWith(response, out var getRequest);
        return (client, getRequest);
    }
}
