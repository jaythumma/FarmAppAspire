using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using FarmAppAspire.CustomerService.Models;

namespace FarmAppAspire.Tests;

/// <summary>
/// Integration tests for Customer API endpoints.
/// Tests POST /customers validation and the billing flag lifecycle.
/// </summary>
public class CustomerEndpointTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
    private const string UserId = "test-user";

    private DistributedApplication? _app;
    private HttpClient? _client;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.FarmAppAspire_AppHost>(cancellationToken);

        _app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await _app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("customerservice", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        _client = _app.CreateHttpClient("customerservice");
        _client.DefaultRequestHeaders.Add("X-User-Id", UserId);
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static AddressFields ValidShippingAddress() => new(
        Line1: "1 Main St", Line2: null,
        City: "Springfield", State: "IL",
        PostalCode: "62701", Country: "US");

    private async Task<CustomerDetailDto> CreateRetailCustomerAsync(
        string name, AddressFields? shipping = null)
    {
        var req = new CreateCustomerRequest(
            CustomerType.Retail, name,
            CompanyName: null, TaxId: null, PaymentTerms: null,
            PrimaryEmail: null, PrimaryPhone: null, Notes: null,
            ShippingAddress: shipping ?? ValidShippingAddress());

        var response = await _client!.PostAsJsonAsync("/customers", req, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions))!;
    }

    // ── POST /customers — required field validation ───────────────────────────

    [Fact]
    public async Task PostCustomer_WithShippingAddress_ReturnsCreated()
    {
        var name = $"Retail-{Guid.NewGuid():N}";
        var created = await CreateRetailCustomerAsync(name);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(name, created.DisplayName);
        Assert.True(created.BillingUsesShipping);
        Assert.Contains(created.Addresses, a => a.Type == AddressType.Shipping);
    }

    [Fact]
    public async Task PostCustomer_WithoutXUserId_ReturnsBadRequest()
    {
        using var clientNoHeader = _app!.CreateHttpClient("customerservice");
        var req = new CreateCustomerRequest(
            CustomerType.Retail, "No-Header",
            null, null, null, null, null, null,
            ShippingAddress: ValidShippingAddress());

        var response = await clientNoHeader.PostAsJsonAsync("/customers", req, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCustomer_Wholesale_WithoutCompanyName_ReturnsBadRequest()
    {
        var req = new CreateCustomerRequest(
            CustomerType.Wholesale, "ACME Wholesale",
            CompanyName: null, TaxId: null, PaymentTerms: null,
            PrimaryEmail: null, PrimaryPhone: null, Notes: null,
            ShippingAddress: ValidShippingAddress());

        var response = await _client!.PostAsJsonAsync("/customers", req, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCustomer_Wholesale_WithCompanyName_ReturnsCreated()
    {
        var name = $"Wholesale-{Guid.NewGuid():N}";
        var req = new CreateCustomerRequest(
            CustomerType.Wholesale, name,
            CompanyName: "ACME Ltd", TaxId: null, PaymentTerms: null,
            PrimaryEmail: null, PrimaryPhone: null, Notes: null,
            ShippingAddress: ValidShippingAddress());

        var response = await _client!.PostAsJsonAsync("/customers", req, JsonOptions);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions);

        Assert.NotNull(created);
        Assert.Equal("ACME Ltd", created.CompanyName);
    }

    [Fact]
    public async Task PostCustomer_WithoutShippingAddress_ReturnsBadRequest()
    {
        // Send a request with an effectively empty shipping address (missing required fields)
        var req = new
        {
            Type = "Retail",
            DisplayName = "No-Address",
            ShippingAddress = new { Line1 = "", City = "", State = "", PostalCode = "", Country = "" },
            BillingUsesShipping = true
        };

        var response = await _client!.PostAsJsonAsync("/customers", req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCustomer_BillingUsesShipping_DefaultsToTrue()
    {
        var name = $"BillingDefault-{Guid.NewGuid():N}";
        var created = await CreateRetailCustomerAsync(name);

        Assert.True(created.BillingUsesShipping);
        Assert.DoesNotContain(created.Addresses, a => a.Type == AddressType.Billing);
    }

    [Fact]
    public async Task PostCustomer_WithBillingAddress_CreatesBillingRecord()
    {
        var name = $"WithBilling-{Guid.NewGuid():N}";
        var billingAddr = new AddressFields(
            "99 Billing Rd", null, "Chicago", "IL", "60601", "US");

        var req = new CreateCustomerRequest(
            CustomerType.Retail, name,
            null, null, null, null, null, null,
            ShippingAddress: ValidShippingAddress(),
            BillingUsesShipping: false,
            BillingAddress: billingAddr);

        var response = await _client!.PostAsJsonAsync("/customers", req, JsonOptions);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions);

        Assert.NotNull(created);
        Assert.False(created.BillingUsesShipping);
        Assert.Contains(created.Addresses, a => a.Type == AddressType.Billing);
        var billing = created.Addresses.First(a => a.Type == AddressType.Billing);
        Assert.Equal("99 Billing Rd", billing.Line1);
    }

    // ── PUT /customers/{id} — Wholesale CompanyName ───────────────────────────

    [Fact]
    public async Task PutCustomer_Wholesale_WithoutCompanyName_ReturnsBadRequest()
    {
        // First create a wholesale customer with company name
        var name = $"WholesalePut-{Guid.NewGuid():N}";
        var req = new CreateCustomerRequest(
            CustomerType.Wholesale, name,
            "Original Corp", null, null, null, null, null,
            ValidShippingAddress());
        var createResp = await _client!.PostAsJsonAsync("/customers", req, JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions);

        // Try to update without CompanyName
        var updateReq = new UpdateCustomerRequest(
            DisplayName: name, CompanyName: null, TaxId: null,
            PaymentTerms: null, PrimaryEmail: null, PrimaryPhone: null, Notes: null);
        var response = await _client.PutAsJsonAsync($"/customers/{created!.Id}", updateReq, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── PUT /customers/{id}/addresses/{aid} — BillingUsesShipping flag ───────

    [Fact]
    public async Task PutAddress_UpdateShipping_WhenBillingUsesShipping_ClearsFlag()
    {
        var name = $"FlagTest-{Guid.NewGuid():N}";
        var created = await CreateRetailCustomerAsync(name);

        // Verify BillingUsesShipping starts as true
        Assert.True(created.BillingUsesShipping);

        var shippingAddress = created.Addresses.First(a => a.Type == AddressType.Shipping);

        // Update the shipping address — this should clear BillingUsesShipping
        var updateAddr = new UpdateAddressRequest(
            shippingAddress.Label,
            AddressType.Shipping,
            "2 Updated St", null,
            shippingAddress.City, shippingAddress.State,
            shippingAddress.PostalCode, shippingAddress.Country,
            shippingAddress.IsDefault);

        var response = await _client!.PutAsJsonAsync(
            $"/customers/{created.Id}/addresses/{shippingAddress.Id}",
            updateAddr, JsonOptions);
        response.EnsureSuccessStatusCode();

        // Fetch updated customer and verify BillingUsesShipping is now false
        var updatedCustomer = await _client.GetFromJsonAsync<CustomerDetailDto>(
            $"/customers/{created.Id}", JsonOptions);

        Assert.NotNull(updatedCustomer);
        Assert.False(updatedCustomer.BillingUsesShipping,
            "BillingUsesShipping should be cleared when shipping address is updated");
    }

    [Fact]
    public async Task PutAddress_UpdateBillingAddress_DoesNotClearFlag()
    {
        // Create customer with explicit billing address (BillingUsesShipping = false)
        var name = $"BillingUpdate-{Guid.NewGuid():N}";
        var req = new CreateCustomerRequest(
            CustomerType.Retail, name,
            null, null, null, null, null, null,
            ValidShippingAddress(),
            BillingUsesShipping: false,
            BillingAddress: new AddressFields("5 Billing St", null, "Peoria", "IL", "61602", "US"));

        var createResp = await _client!.PostAsJsonAsync("/customers", req, JsonOptions);
        var created = await createResp.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions);

        Assert.NotNull(created);
        Assert.False(created.BillingUsesShipping);

        var billingAddr = created.Addresses.First(a => a.Type == AddressType.Billing);

        // Update the billing address
        var updateAddr = new UpdateAddressRequest(
            billingAddr.Label,
            AddressType.Billing,
            "6 New Billing St", null,
            billingAddr.City, billingAddr.State,
            billingAddr.PostalCode, billingAddr.Country,
            billingAddr.IsDefault);

        var response = await _client.PutAsJsonAsync(
            $"/customers/{created.Id}/addresses/{billingAddr.Id}",
            updateAddr, JsonOptions);
        response.EnsureSuccessStatusCode();

        // BillingUsesShipping should remain false (billing address update doesn't affect it)
        var updatedCustomer = await _client.GetFromJsonAsync<CustomerDetailDto>(
            $"/customers/{created.Id}", JsonOptions);

        Assert.NotNull(updatedCustomer);
        Assert.False(updatedCustomer.BillingUsesShipping,
            "Updating a billing address should not change BillingUsesShipping flag");
    }

    // ── GET /customers — basic list ───────────────────────────────────────────

    [Fact]
    public async Task GetCustomers_ReturnsOk()
    {
        var response = await _client!.GetAsync("/customers");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCustomer_AfterCreate_ReturnsCorrectData()
    {
        var name = $"GetTest-{Guid.NewGuid():N}";
        var created = await CreateRetailCustomerAsync(name);

        var fetched = await _client!.GetFromJsonAsync<CustomerDetailDto>(
            $"/customers/{created.Id}", JsonOptions);

        Assert.NotNull(fetched);
        Assert.Equal(name, fetched.DisplayName);
        Assert.Equal(CustomerType.Retail, fetched.Type);
        Assert.True(fetched.BillingUsesShipping);
    }

    [Fact]
    public async Task GetCustomer_NotFound_Returns404()
    {
        var response = await _client!.GetAsync($"/customers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── DELETE /customers ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCustomer_ExistingCustomer_ReturnsNoContent()
    {
        var name = $"DeleteTest-{Guid.NewGuid():N}";
        var created = await CreateRetailCustomerAsync(name);

        var response = await _client!.DeleteAsync($"/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Confirm it's gone
        var getResp = await _client.GetAsync($"/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    // ── Address management ────────────────────────────────────────────────────

    [Fact]
    public async Task PostAddress_ThenDeleteDefault_PromotesNextAddress()
    {
        var name = $"AddrPromo-{Guid.NewGuid():N}";
        var customer = await CreateRetailCustomerAsync(name);

        // Add a second address
        var addReq = new CreateAddressRequest(
            "Second", AddressType.Billing,
            "10 Second St", null, "Chicago", "IL", "60601", "US",
            IsDefault: false);
        var addResp = await _client!.PostAsJsonAsync(
            $"/customers/{customer.Id}/addresses", addReq, JsonOptions);
        var second = await addResp.Content.ReadFromJsonAsync<AddressDto>(JsonOptions);

        // Delete the default (shipping) address
        var shippingAddr = customer.Addresses.First(a => a.IsDefault);
        await _client.DeleteAsync($"/customers/{customer.Id}/addresses/{shippingAddr.Id}");

        // Remaining address should now be default
        var remaining = await _client.GetFromJsonAsync<AddressDto[]>(
            $"/customers/{customer.Id}/addresses", JsonOptions);

        Assert.NotNull(remaining);
        Assert.Single(remaining);
        Assert.True(remaining[0].IsDefault, "After deleting the default, the next address should be promoted");
    }

    // ── ChannelType ───────────────────────────────────────────────────────────

    [Fact]
    public async Task PostCustomer_WithoutChannelType_DefaultsToDirect()
    {
        var name = $"ChannelDefault-{Guid.NewGuid():N}";
        var created = await CreateRetailCustomerAsync(name);

        Assert.Equal(ChannelType.Direct, created.ChannelType);
    }

    [Fact]
    public async Task PostCustomer_WithChannelTypeAmazon_SavesAmazon()
    {
        var name = $"ChannelAmazon-{Guid.NewGuid():N}";
        var req = new CreateCustomerRequest(
            CustomerType.Retail, name,
            CompanyName: null, TaxId: null, PaymentTerms: null,
            PrimaryEmail: null, PrimaryPhone: null, Notes: null,
            ShippingAddress: ValidShippingAddress(),
            ChannelType: ChannelType.Amazon);

        var response = await _client!.PostAsJsonAsync("/customers", req, JsonOptions);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions);

        Assert.NotNull(created);
        Assert.Equal(ChannelType.Amazon, created.ChannelType);
    }

    [Fact]
    public async Task PostCustomer_WithChannelTypeAmazon_AutoGeneratesCustomerKey()
    {
        var name = $"AmazonKey-{Guid.NewGuid():N}";
        var req = new CreateCustomerRequest(
            CustomerType.Retail, name,
            CompanyName: null, TaxId: null, PaymentTerms: null,
            PrimaryEmail: null, PrimaryPhone: null, Notes: null,
            ShippingAddress: ValidShippingAddress(),
            ChannelType: ChannelType.Amazon);

        var response = await _client!.PostAsJsonAsync("/customers", req, JsonOptions);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions);

        Assert.NotNull(created);
        Assert.NotNull(created.CustomerKey);
        Assert.StartsWith("AMZ-", created.CustomerKey);
    }

    [Fact]
    public async Task PutCustomer_UpdateChannelTypeToAmazon_AutoGeneratesCustomerKey()
    {
        var name = $"AmazonUpdate-{Guid.NewGuid():N}";
        var created = await CreateRetailCustomerAsync(name);
        Assert.Equal(ChannelType.Direct, created.ChannelType);

        var updateReq = new UpdateCustomerRequest(
            DisplayName: name,
            CompanyName: null, TaxId: null, PaymentTerms: null,
            PrimaryEmail: null, PrimaryPhone: null, Notes: null,
            ChannelType: ChannelType.Amazon);

        var putResp = await _client!.PutAsJsonAsync($"/customers/{created.Id}", updateReq, JsonOptions);
        putResp.EnsureSuccessStatusCode();
        var updated = await putResp.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions);

        Assert.NotNull(updated);
        Assert.Equal(ChannelType.Amazon, updated.ChannelType);
        Assert.NotNull(updated.CustomerKey);
        Assert.StartsWith("AMZ-", updated.CustomerKey);
    }

    [Fact]
    public async Task PutCustomer_UpdateChannelType_Persists()
    {
        var name = $"ChannelUpdate-{Guid.NewGuid():N}";
        var created = await CreateRetailCustomerAsync(name);
        Assert.Equal(ChannelType.Direct, created.ChannelType);

        var updateReq = new UpdateCustomerRequest(
            DisplayName: name,
            CompanyName: null, TaxId: null, PaymentTerms: null,
            PrimaryEmail: null, PrimaryPhone: null, Notes: null,
            ChannelType: ChannelType.Amazon);

        var putResp = await _client!.PutAsJsonAsync($"/customers/{created.Id}", updateReq, JsonOptions);
        putResp.EnsureSuccessStatusCode();
        var updated = await putResp.Content.ReadFromJsonAsync<CustomerDetailDto>(JsonOptions);

        Assert.NotNull(updated);
        Assert.Equal(ChannelType.Amazon, updated.ChannelType);
    }
}
