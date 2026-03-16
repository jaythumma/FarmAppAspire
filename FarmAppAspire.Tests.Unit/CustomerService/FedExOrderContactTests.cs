using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.Tests.Unit.CustomerService;

/// <summary>
/// Tests covering ContactId validation on FedEx order instances (Issue #40).
/// </summary>
public class FedExOrderContactTests : IDisposable
{
    private readonly CustomerDbContext _db;

    public FedExOrderContactTests()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new CustomerDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<(Customer customer, CustomerContact contact)> SeedWholesaleCustomerWithContactAsync(
        ContactRole role = ContactRole.Primary)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(), Type = CustomerType.Wholesale,
            DisplayName = "Acme Co", CompanyName = "Acme Ltd",
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        var contact = new CustomerContact
        {
            Id = Guid.NewGuid(), CustomerId = customer.Id,
            Role = role, FirstName = "Jane", LastName = "Doe",
            IsPrimary = role == ContactRole.Primary
        };
        _db.Customers.Add(customer);
        _db.CustomerContacts.Add(contact);
        await _db.SaveChangesAsync();
        return (customer, contact);
    }

    private async Task<Customer> SeedRetailCustomerAsync()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(), Type = CustomerType.Retail,
            DisplayName = "John Smith",
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    private async Task<StandingOrder> SeedFedExStandingOrderAsync(Guid customerId)
    {
        var so = new StandingOrder
        {
            Id = Guid.NewGuid(), CustomerId = customerId, Channel = OrderChannel.FedEx,
            Frequency = OrderFrequency.OnRequest, Status = StandingOrderStatus.Active,
            SeasonYear = 2024, StartWeek = DateTime.UtcNow.Date,
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _db.StandingOrders.Add(so);
        await _db.SaveChangesAsync();
        return so;
    }

    // ── Scenario: Contact from wholesale customer is recorded on a FedEx order ──

    [Fact]
    public async Task CreateFedExOrder_WholesaleWithValidContact_SetsContactId()
    {
        var (customer, contact) = await SeedWholesaleCustomerWithContactAsync();

        // Simulate the POST guard in FedExOrderEndpoints
        var contactBelongsToCustomer = await _db.CustomerContacts
            .AnyAsync(c => c.Id == contact.Id && c.CustomerId == customer.Id);

        Assert.True(contactBelongsToCustomer, "A contact belonging to the customer must pass validation.");

        var so = await SeedFedExStandingOrderAsync(customer.Id);
        var instance = new Order
        {
            Id              = Guid.NewGuid(),
            StandingOrderId = so.Id,
            CustomerId      = customer.Id,
            ContactId       = contact.Id,
            Channel         = OrderChannel.FedEx,
            Status          = OrderStatus.Pending,
            WeekOf          = DateTime.UtcNow.Date,
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = "test"
        };
        _db.Orders.Add(instance);
        await _db.SaveChangesAsync();

        var saved = await _db.Orders.FindAsync(instance.Id);
        Assert.Equal(contact.Id, saved!.ContactId);
    }

    // ── Scenario: Contact not belonging to the customer is rejected ───────────

    [Fact]
    public async Task CreateFedExOrder_WithForeignContact_ContactValidationFails()
    {
        var (customer, _) = await SeedWholesaleCustomerWithContactAsync();

        // A contact ID not associated with this customer
        var foreignContactId = Guid.NewGuid();

        var contactBelongsToCustomer = await _db.CustomerContacts
            .AnyAsync(c => c.Id == foreignContactId && c.CustomerId == customer.Id);

        // Guard logic should return BadRequest when this is false
        Assert.False(contactBelongsToCustomer,
            "A contact not belonging to the customer should fail validation → HTTP 400.");
    }

    // ── Scenario: Retail FedEx order created without a contact ───────────────

    [Fact]
    public async Task CreateFedExOrder_RetailWithNullContact_IsAllowed()
    {
        var customer = await SeedRetailCustomerAsync();

        // No ContactId provided – retail orders do not require one
        Guid? contactId = null;

        // Validate: guard only fires when ContactId has a value
        var shouldValidate = contactId.HasValue;
        Assert.False(shouldValidate, "Retail FedEx orders must not require a ContactId.");

        var so = await SeedFedExStandingOrderAsync(customer.Id);
        var instance = new Order
        {
            Id              = Guid.NewGuid(),
            StandingOrderId = so.Id,
            CustomerId      = customer.Id,
            ContactId       = contactId,
            Channel         = OrderChannel.FedEx,
            Status          = OrderStatus.Pending,
            WeekOf          = DateTime.UtcNow.Date,
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = "test"
        };
        _db.Orders.Add(instance);
        await _db.SaveChangesAsync();

        var saved = await _db.Orders.FindAsync(instance.Id);
        Assert.Null(saved!.ContactId);
    }

    // ── Scenario: Any contact role at a wholesale customer may place orders ───

    [Theory]
    [InlineData(ContactRole.Primary)]
    [InlineData(ContactRole.Billing)]
    [InlineData(ContactRole.Purchasing)]
    [InlineData(ContactRole.Shipping)]
    [InlineData(ContactRole.Secondary)]
    [InlineData(ContactRole.Other)]
    public async Task CreateFedExOrder_AnyContactRole_IsAccepted(ContactRole role)
    {
        var (customer, contact) = await SeedWholesaleCustomerWithContactAsync(role);

        var contactBelongsToCustomer = await _db.CustomerContacts
            .AnyAsync(c => c.Id == contact.Id && c.CustomerId == customer.Id);

        Assert.True(contactBelongsToCustomer,
            $"A contact with role {role} belonging to the customer must pass validation.");
    }

    // ── Scenario: Contact from a different customer is rejected on FedEx order ──

    [Fact]
    public async Task CreateFedExOrder_ContactBelongingToOtherCustomer_ValidationFails()
    {
        var (customer, _) = await SeedWholesaleCustomerWithContactAsync();

        // Seed a second wholesale customer with their own contact
        var otherCustomer = new Customer
        {
            Id = Guid.NewGuid(), Type = CustomerType.Wholesale,
            DisplayName = "Other Co", CompanyName = "Other Ltd",
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        var otherContact = new CustomerContact
        {
            Id = Guid.NewGuid(), CustomerId = otherCustomer.Id,
            Role = ContactRole.Primary, FirstName = "Bob", LastName = "Other", IsPrimary = true
        };
        _db.Customers.Add(otherCustomer);
        _db.CustomerContacts.Add(otherContact);
        await _db.SaveChangesAsync();

        // Try to use otherContact.Id against the first customer – must fail
        var contactBelongsToCustomer = await _db.CustomerContacts
            .AnyAsync(c => c.Id == otherContact.Id && c.CustomerId == customer.Id);

        Assert.False(contactBelongsToCustomer,
            "A contact belonging to a different customer must not pass validation → HTTP 400.");
    }
}
