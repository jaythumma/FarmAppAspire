using FarmAppAspire.CustomerService.Models;

namespace FarmAppAspire.Tests.Unit.CustomerService;

public class CustomerMappingTests
{
    private static Customer BuildCustomer(
        IList<CustomerContact>? contacts = null,
        IList<CustomerAddress>? addresses = null) => new()
    {
        Id = Guid.NewGuid(),
        Type = CustomerType.Wholesale,
        DisplayName = "Test Corp",
        CompanyName = "Test Corp Ltd",
        TaxId = "TAX-123",
        PaymentTerms = PaymentTerms.NET30,
        PrimaryEmail = "test@corp.com",
        PrimaryPhone = "555-1234",
        Notes = "VIP customer",
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "seed-user",
        ModifiedAt = null,
        ModifiedBy = null,
        Contacts = contacts ?? [],
        Addresses = addresses ?? []
    };

    [Fact]
    public void ToSummaryDto_MapsAllFields()
    {
        var customer = BuildCustomer();

        var dto = customer.ToSummaryDto();

        Assert.Equal(customer.Id, dto.Id);
        Assert.Equal(customer.Type, dto.Type);
        Assert.Equal(customer.DisplayName, dto.DisplayName);
        Assert.Equal(customer.CompanyName, dto.CompanyName);
        Assert.Equal(customer.PrimaryEmail, dto.PrimaryEmail);
        Assert.Equal(customer.PrimaryPhone, dto.PrimaryPhone);
    }

    [Fact]
    public void ToDetailDto_SortsContacts_PrimaryFirst()
    {
        var secondary = new CustomerContact { Id = Guid.NewGuid(), Role = ContactRole.Secondary, FirstName = "B", LastName = "B", IsPrimary = false };
        var primary = new CustomerContact { Id = Guid.NewGuid(), Role = ContactRole.Primary, FirstName = "A", LastName = "A", IsPrimary = true };

        var customer = BuildCustomer(contacts: [secondary, primary]);

        var dto = customer.ToDetailDto();

        Assert.Equal(primary.Id, dto.Contacts.First().Id);
    }

    [Fact]
    public void ToDetailDto_SortsAddresses_DefaultFirst()
    {
        var nonDefault = new CustomerAddress { Id = Guid.NewGuid(), Label = "Secondary", Type = AddressType.Billing, Line1 = "B St", City = "B", State = "B", PostalCode = "B", Country = "US", IsDefault = false };
        var defaultAddr = new CustomerAddress { Id = Guid.NewGuid(), Label = "Primary", Type = AddressType.Shipping, Line1 = "A St", City = "A", State = "A", PostalCode = "A", Country = "US", IsDefault = true };

        var customer = BuildCustomer(addresses: [nonDefault, defaultAddr]);

        var dto = customer.ToDetailDto();

        Assert.Equal(defaultAddr.Id, dto.Addresses.First().Id);
    }

    [Fact]
    public void ToDetailDto_HandlesEmptyChildCollections()
    {
        var customer = BuildCustomer();

        var dto = customer.ToDetailDto();

        Assert.NotNull(dto.Contacts);
        Assert.NotNull(dto.Addresses);
        Assert.Empty(dto.Contacts);
        Assert.Empty(dto.Addresses);
    }

    [Fact]
    public void ToDetailDto_MapsBillingUsesShipping()
    {
        var customer = BuildCustomer();
        customer.BillingUsesShipping = true;

        var dto = customer.ToDetailDto();

        Assert.True(dto.BillingUsesShipping);
    }

    [Fact]
    public void ToDetailDto_MapsCustomerKey_WhenSet()
    {
        var customer = BuildCustomer();
        customer.CustomerKey = "TES-SPR-IL";
        customer.CustomerKeyCollision = false;

        var dto = customer.ToDetailDto();

        Assert.Equal("TES-SPR-IL", dto.CustomerKey);
        Assert.False(dto.CustomerKeyCollision);
    }

    [Fact]
    public void ToDetailDto_MapsCustomerKey_NullWhenNotSet()
    {
        var customer = BuildCustomer();
        customer.CustomerKey = null;
        customer.CustomerKeyCollision = false;

        var dto = customer.ToDetailDto();

        Assert.Null(dto.CustomerKey);
        Assert.False(dto.CustomerKeyCollision);
    }

    [Fact]
    public void ToDetailDto_MapsCustomerKeyCollision_WhenTrue()
    {
        var customer = BuildCustomer();
        customer.CustomerKey = "TES-SPR-IL";
        customer.CustomerKeyCollision = true;

        var dto = customer.ToDetailDto();

        Assert.Equal("TES-SPR-IL", dto.CustomerKey);
        Assert.True(dto.CustomerKeyCollision);
    }

    [Fact]
    public void ContactToDto_MapsAllFields()
    {
        var contact = new CustomerContact
        {
            Id = Guid.NewGuid(),
            Role = ContactRole.Billing,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@corp.com",
            Phone = "555-0001",
            Mobile = "555-0002",
            IsPrimary = true
        };

        var dto = contact.ToDto();

        Assert.Equal(contact.Id, dto.Id);
        Assert.Equal(contact.Role, dto.Role);
        Assert.Equal(contact.FirstName, dto.FirstName);
        Assert.Equal(contact.LastName, dto.LastName);
        Assert.Equal(contact.Email, dto.Email);
        Assert.Equal(contact.Phone, dto.Phone);
        Assert.Equal(contact.Mobile, dto.Mobile);
        Assert.Equal(contact.IsPrimary, dto.IsPrimary);
    }

    [Fact]
    public void AddressToDto_MapsAllFields()
    {
        var address = new CustomerAddress
        {
            Id = Guid.NewGuid(),
            Label = "HQ",
            Type = AddressType.Shipping,
            Line1 = "1 Main St",
            Line2 = "Suite 100",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "US",
            IsDefault = true
        };

        var dto = address.ToDto();

        Assert.Equal(address.Id, dto.Id);
        Assert.Equal(address.Label, dto.Label);
        Assert.Equal(address.Type, dto.Type);
        Assert.Equal(address.Line1, dto.Line1);
        Assert.Equal(address.Line2, dto.Line2);
        Assert.Equal(address.City, dto.City);
        Assert.Equal(address.State, dto.State);
        Assert.Equal(address.PostalCode, dto.PostalCode);
        Assert.Equal(address.Country, dto.Country);
        Assert.Equal(address.IsDefault, dto.IsDefault);
    }
}
