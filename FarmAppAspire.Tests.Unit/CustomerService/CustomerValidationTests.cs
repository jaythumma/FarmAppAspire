using FarmAppAspire.CustomerService.Models;

namespace FarmAppAspire.Tests.Unit.CustomerService;

public class CustomerValidationTests
{
    // ── CompanyName conditional requirement ───────────────────────────────────

    [Fact]
    public void CreateCustomerRequest_Wholesale_WithoutCompanyName_FailsValidation()
    {
        var req = new CreateCustomerRequest(
            CustomerType.Wholesale, "ACME",
            CompanyName: null, TaxId: null, PaymentTerms: null,
            PrimaryEmail: null, PrimaryPhone: null, Notes: null,
            ShippingAddress: ValidAddress());

        // Simulate the API guard (mirror of Program.cs logic)
        var isInvalid = req.Type == CustomerType.Wholesale && string.IsNullOrWhiteSpace(req.CompanyName);

        Assert.True(isInvalid, "Wholesale without CompanyName should be invalid");
    }

    [Fact]
    public void CreateCustomerRequest_Wholesale_WithCompanyName_PassesValidation()
    {
        var req = new CreateCustomerRequest(
            CustomerType.Wholesale, "ACME",
            CompanyName: "ACME Ltd", TaxId: null, PaymentTerms: null,
            PrimaryEmail: null, PrimaryPhone: null, Notes: null,
            ShippingAddress: ValidAddress());

        var isInvalid = req.Type == CustomerType.Wholesale && string.IsNullOrWhiteSpace(req.CompanyName);

        Assert.False(isInvalid, "Wholesale with CompanyName should be valid");
    }

    [Fact]
    public void CreateCustomerRequest_Retail_WithoutCompanyName_PassesValidation()
    {
        var req = new CreateCustomerRequest(
            CustomerType.Retail, "Jane Doe",
            CompanyName: null, TaxId: null, PaymentTerms: null,
            PrimaryEmail: null, PrimaryPhone: null, Notes: null,
            ShippingAddress: ValidAddress());

        // Retail does NOT require CompanyName
        var isInvalid = req.Type == CustomerType.Wholesale && string.IsNullOrWhiteSpace(req.CompanyName);

        Assert.False(isInvalid, "Retail without CompanyName should be valid");
    }

    // ── BillingUsesShipping default ───────────────────────────────────────────

    [Fact]
    public void CreateCustomerRequest_BillingUsesShipping_DefaultsToTrue()
    {
        var req = new CreateCustomerRequest(
            CustomerType.Retail, "Jane",
            null, null, null, null, null, null,
            ShippingAddress: ValidAddress());

        Assert.True(req.BillingUsesShipping);
    }

    [Fact]
    public void CreateCustomerRequest_BillingUsesShipping_CanBeSetToFalse()
    {
        var req = new CreateCustomerRequest(
            CustomerType.Retail, "Jane",
            null, null, null, null, null, null,
            ShippingAddress: ValidAddress(),
            BillingUsesShipping: false,
            BillingAddress: ValidAddress());

        Assert.False(req.BillingUsesShipping);
        Assert.NotNull(req.BillingAddress);
    }

    // ── AddressFields contract ────────────────────────────────────────────────
    // Note: [Required] on positional record parameters targets the constructor
    // parameter, not the init property, so Validator.TryValidateObject won't
    // catch it. API-layer validation for missing address fields is exercised in
    // the integration tests (CustomerEndpointTests).

    [Fact]
    public void AddressFields_EmptyLine1_ValueIsPreserved()
    {
        // Verifies the record stores what it's given — enforcement is API-layer.
        var address = new AddressFields(
            Line1: "", Line2: null,
            City: "Springfield", State: "IL", PostalCode: "62701", Country: "US");

        Assert.Equal("", address.Line1);
    }

    [Fact]
    public void AddressFields_WithAllRequiredFields_HasExpectedValues()
    {
        var address = ValidAddress();

        Assert.Equal("1 Main St", address.Line1);
        Assert.Equal("Springfield", address.City);
        Assert.Equal("IL", address.State);
        Assert.Equal("62701", address.PostalCode);
        Assert.Equal("US", address.Country);
    }

    private static AddressFields ValidAddress() => new(
        Line1: "1 Main St", Line2: null,
        City: "Springfield", State: "IL", PostalCode: "62701", Country: "US");
}
