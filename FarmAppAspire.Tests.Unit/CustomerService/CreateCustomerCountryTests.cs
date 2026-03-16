using System.Net.Http.Json;
using FarmAppAspire.Web;
using Xunit;

namespace FarmAppAspire.Tests.Unit.CustomerService;

public class CreateCustomerCountryTests
{
    [Fact]
    public async Task PostingUnitedStatesCountry_Succeeds()
    {
        // Arrange: create minimal CreateCustomerRequest with full country name
        var client = TestClients.ApiClient();

        var createReq = new CreateCustomerRequest(
            Type: CustomerType.Retail,
            DisplayName: "Test Customer",
            CompanyName: null,
            TaxId: null,
            PaymentTerms: null,
            PrimaryEmail: null,
            PrimaryPhone: null,
            Notes: null,
            ShippingAddress: new AddressFields(
                Line1: "1 Main St",
                Line2: null,
                City: "Springfield",
                State: "IL",
                PostalCode: "62701",
                Country: "United States"),
            BillingUsesShipping: true,
            BillingAddress: null,
            ChannelType: ChannelType.Direct
        );

        // Act
        var resp = await client.PostAsJsonAsync("/customers", createReq);

        // Assert
        resp.EnsureSuccessStatusCode();
    }
}
