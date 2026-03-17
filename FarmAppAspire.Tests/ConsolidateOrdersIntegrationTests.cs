using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;

namespace FarmAppAspire.Tests;

/// <summary>
/// Integration tests validating the consolidate-orders-ui change:
/// - FedEx orders have null StandingOrderId and do not create StandingOrders
/// - /orders endpoint supports weekOf and channel filters
/// - /standing-orders redirects to /orders (no 404)
/// </summary>
public class ConsolidateOrdersIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(90);
    private const string UserId = "integration-test";

    private DistributedApplication? _app;
    private HttpClient? _customerClient;
    private HttpClient? _webClient;

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.FarmAppAspire_AppHost>(ct);

        _app = await appHost.BuildAsync(ct).WaitAsync(DefaultTimeout, ct);
        await _app.StartAsync(ct).WaitAsync(DefaultTimeout, ct);

        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("customerservice", ct)
            .WaitAsync(DefaultTimeout, ct);
        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("webfrontend", ct)
            .WaitAsync(DefaultTimeout, ct);

        _customerClient = _app.CreateHttpClient("customerservice");
        _customerClient.DefaultRequestHeaders.Add("X-User-Id", UserId);

        _webClient = _app.CreateHttpClient("webfrontend");
    }

    public async ValueTask DisposeAsync()
    {
        _customerClient?.Dispose();
        _webClient?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CreateCustomerRequest NewRetailCustomer(string name) => new(
        CustomerType.Retail, name, null, null, null, null, null, null,
        ShippingAddress: new AddressFields("1 Farm Rd", null, "Springfield", "IL", "62701", "US"));

    private async Task<CustomerDetailDto> CreateCustomerAsync(string name)
    {
        var resp = await _customerClient!.PostAsJsonAsync("/customers", NewRetailCustomer(name), JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions))!;
    }

    private async Task<StandingOrderDto> CreateInsulatedSOAsync(Guid customerId)
    {
        var req = new CreateStandingOrderRequest(
            OrderChannel.Insulated, null, OrderFrequency.Weekly, null, false,
            [new StandingOrderLineRequest(InsulatedBoxSize.TwelveLb, 1)]);
        var resp = await _customerClient!.PostAsJsonAsync(
            $"/customers/{customerId}/standing-orders", req, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<StandingOrderDto>(JsonOptions))!;
    }

    private async Task<OrderDto> CreateFedExOrderAsync(Guid customerId)
    {
        var req = new CreateOrderRequest(
            OrderChannel.FedEx, DateTime.UtcNow,
            Lines: [new CreateOrderLineRequest(null, FedExTierSize.FourOz, 1)]);
        var resp = await _customerClient!.PostAsJsonAsync(
            $"/customers/{customerId}/orders", req, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<OrderDto>(JsonOptions))!;
    }

    // ── Task 7.2: FedEx order — null StandingOrderId, no StandingOrder created ─

    [Fact]
    public async Task FedExOrder_HasNullStandingOrderId()
    {
        var customer = await CreateCustomerAsync($"FedExNull-{Guid.NewGuid():N}");

        var order = await CreateFedExOrderAsync(customer.Id);

        Assert.Equal(OrderChannel.FedEx, order.Channel);
        Assert.Null(order.StandingOrderId);
    }

    [Fact]
    public async Task FedExOrder_DoesNotCreateStandingOrder()
    {
        var customer = await CreateCustomerAsync($"FedExNoSO-{Guid.NewGuid():N}");

        var countBefore = (await _customerClient!.GetFromJsonAsync<StandingOrderDto[]>(
            $"/customers/{customer.Id}/standing-orders", JsonOptions))?.Length ?? 0;

        await CreateFedExOrderAsync(customer.Id);

        var countAfter = (await _customerClient!.GetFromJsonAsync<StandingOrderDto[]>(
            $"/customers/{customer.Id}/standing-orders", JsonOptions))?.Length ?? 0;

        Assert.Equal(countBefore, countAfter);
    }

    [Fact]
    public async Task MultipleFedExOrders_AllHaveNullStandingOrderId()
    {
        var customer = await CreateCustomerAsync($"FedExMulti-{Guid.NewGuid():N}");

        var o1 = await CreateFedExOrderAsync(customer.Id);
        var o2 = await CreateFedExOrderAsync(customer.Id);

        Assert.Null(o1.StandingOrderId);
        Assert.Null(o2.StandingOrderId);
    }

    // ── Task 7.1: Customer Detail — Insulated SO accessible via API ───────────

    [Fact]
    public async Task CustomerInsulatedSchedule_ReturnedByStandingOrdersEndpoint()
    {
        var ct       = TestContext.Current.CancellationToken;
        var customer = await CreateCustomerAsync($"InsSO-{Guid.NewGuid():N}");
        await CreateInsulatedSOAsync(customer.Id);

        var sos = await _customerClient!.GetFromJsonAsync<StandingOrderDto[]>(
            $"/customers/{customer.Id}/standing-orders", JsonOptions, ct);

        var insulated = sos?.FirstOrDefault(s => s.Channel == OrderChannel.Insulated);
        Assert.NotNull(insulated);
        Assert.Equal(StandingOrderStatus.Active, insulated.Status);
        Assert.Single(insulated.Lines);
    }

    // ── Task 7.3: /orders weekOf filter ──────────────────────────────────────

    [Fact]
    public async Task GetOrders_WithWeekOfFilter_ReturnsOnlyThatWeeksOrders()
    {
        var ct       = TestContext.Current.CancellationToken;
        var customer = await CreateCustomerAsync($"WeekFilter-{Guid.NewGuid():N}");
        await CreateInsulatedSOAsync(customer.Id);

        // Generate orders for this week
        var weekOf = SeasonYearService.MondayOf(DateTime.UtcNow);
        var genResp = await _customerClient!.PostAsJsonAsync(
            "/admin/generate-orders", new GenerateOrdersRequest(weekOf), JsonOptions, ct);
        genResp.EnsureSuccessStatusCode();

        // Query with weekOf + customerId filters
        var weekStr = weekOf.ToString("yyyy-MM-dd");
        var orders  = await _customerClient!.GetFromJsonAsync<AllOrdersSummaryDto[]>(
            $"/orders?weekOf={weekStr}&customerId={customer.Id}", JsonOptions, ct);

        Assert.NotNull(orders);
        Assert.All(orders, o =>
        {
            Assert.True(o.WeekOf >= weekOf && o.WeekOf < weekOf.AddDays(7),
                $"Order WeekOf {o.WeekOf} outside week {weekStr}");
        });
    }

    [Fact]
    public async Task GetOrders_WithChannelFilter_ReturnsOnlyThatChannel()
    {
        var ct       = TestContext.Current.CancellationToken;
        var customer = await CreateCustomerAsync($"ChanFilter-{Guid.NewGuid():N}");
        await CreateFedExOrderAsync(customer.Id);

        var orders = await _customerClient!.GetFromJsonAsync<AllOrdersSummaryDto[]>(
            $"/orders?customerId={customer.Id}&channel=FedEx", JsonOptions, ct);

        Assert.NotNull(orders);
        Assert.NotEmpty(orders);
        Assert.All(orders, o => Assert.Equal("FedEx", o.Channel));
    }

    // ── Task 7.5: /standing-orders redirects — no 404 ─────────────────────────

    [Fact]
    public async Task StandingOrdersRoute_IsNotA404()
    {
        var ct = TestContext.Current.CancellationToken;

        // Blazor server renders the redirect component — page exists, no 404
        var response = await _webClient!.GetAsync("/standing-orders", ct);

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
