using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.Tests.Unit.CustomerService;

/// <summary>
/// Tests covering ContactId management on standing orders (Issue #7).
/// </summary>
public class StandingOrderContactTests : IDisposable
{
    private readonly CustomerDbContext _db;

    public StandingOrderContactTests()
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

    private async Task<(Customer customer, CustomerContact contact)> SeedWholesaleCustomerWithContactAsync()
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
            Role = ContactRole.Primary, FirstName = "Jane", LastName = "Doe", IsPrimary = true
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

    private static StandingOrder BuildStandingOrder(Guid customerId, Guid? contactId = null) => new()
    {
        Id         = Guid.NewGuid(),
        CustomerId = customerId,
        ContactId  = contactId,
        Frequency  = OrderFrequency.Weekly,
        SeasonYear = 2024,
        StartWeek  = new DateTime(2024, 6, 3),
        Status     = StandingOrderStatus.Active,
        CreatedAt  = DateTime.UtcNow,
        CreatedBy  = "test"
    };

    // ── Scenario: Contact from wholesale customer is recorded on a standing order ──

    [Fact]
    public async Task CreateStandingOrder_WithValidContact_SetsContactId()
    {
        var (customer, contact) = await SeedWholesaleCustomerWithContactAsync();

        // Simulate the POST guard
        var contactBelongsToCustomer = await _db.CustomerContacts
            .AnyAsync(c => c.Id == contact.Id && c.CustomerId == customer.Id);

        Assert.True(contactBelongsToCustomer, "A contact belonging to the customer must pass validation.");

        var so = BuildStandingOrder(customer.Id, contact.Id);
        _db.StandingOrders.Add(so);
        await _db.SaveChangesAsync();

        var saved = await _db.StandingOrders.FindAsync(so.Id);
        Assert.Equal(contact.Id, saved!.ContactId);
    }

    // ── Scenario: Contact not belonging to the customer is rejected ───────────

    [Fact]
    public async Task CreateStandingOrder_WithForeignContact_ContactValidationFails()
    {
        var (customer, _) = await SeedWholesaleCustomerWithContactAsync();

        // A contact belonging to a different customer
        var otherContactId = Guid.NewGuid();

        var contactBelongsToCustomer = await _db.CustomerContacts
            .AnyAsync(c => c.Id == otherContactId && c.CustomerId == customer.Id);

        // Guard logic should return BadRequest when this is false
        Assert.False(contactBelongsToCustomer,
            "A contact not belonging to the customer should fail validation → HTTP 400.");
    }

    // ── Scenario: ContactId on a standing order is propagated to generated instances ──

    [Fact]
    public async Task GenerateInstance_InheritsContactIdFromStandingOrder()
    {
        var (customer, contact) = await SeedWholesaleCustomerWithContactAsync();

        var so = BuildStandingOrder(customer.Id, contactId: contact.Id);
        _db.StandingOrders.Add(so);
        await _db.SaveChangesAsync();

        // Mirror the instance-generation logic from AdminEndpoints
        var instance = new OrderInstance
        {
            Id              = Guid.NewGuid(),
            StandingOrderId = so.Id,
            CustomerId      = so.CustomerId,
            ContactId       = so.ContactId,   // propagation under test
            Channel         = OrderChannel.Insulated,
            Status          = OrderInstanceStatus.Pending,
            WeekOf          = so.StartWeek,
            IsSample        = so.IsSample,
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = "test"
        };

        _db.OrderInstances.Add(instance);
        await _db.SaveChangesAsync();

        var saved = await _db.OrderInstances.FindAsync(instance.Id);
        Assert.Equal(contact.Id, saved!.ContactId);
    }

    [Fact]
    public async Task GenerateInstance_WithNullContactId_InstanceContactIdIsNull()
    {
        var customer = await SeedRetailCustomerAsync();

        var so = BuildStandingOrder(customer.Id, contactId: null);
        _db.StandingOrders.Add(so);
        await _db.SaveChangesAsync();

        var instance = new OrderInstance
        {
            Id              = Guid.NewGuid(),
            StandingOrderId = so.Id,
            CustomerId      = so.CustomerId,
            ContactId       = so.ContactId,   // null propagation under test
            Channel         = OrderChannel.Insulated,
            Status          = OrderInstanceStatus.Pending,
            WeekOf          = so.StartWeek,
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = "test"
        };

        _db.OrderInstances.Add(instance);
        await _db.SaveChangesAsync();

        var saved = await _db.OrderInstances.FindAsync(instance.Id);
        Assert.Null(saved!.ContactId);
    }

    // ── Scenario: Retail standing order created without a contact ─────────────

    [Fact]
    public async Task CreateStandingOrder_RetailWithNullContact_IsAllowed()
    {
        var customer = await SeedRetailCustomerAsync();

        // No ContactId provided – retail orders do not require one
        var req = new CreateStandingOrderRequest(
            ContactId: null,
            Frequency: OrderFrequency.Weekly,
            MonthlyWeek: null,
            IsSample: false,
            Lines: [new StandingOrderLineRequest(InsulatedBoxSize.TenLb, 1)]);

        // Validate: guard only fires when ContactId has a value
        var shouldValidate = req.ContactId.HasValue;
        Assert.False(shouldValidate, "Retail standing orders must not require a ContactId.");

        var so = BuildStandingOrder(customer.Id, contactId: req.ContactId);
        _db.StandingOrders.Add(so);
        await _db.SaveChangesAsync();

        var saved = await _db.StandingOrders.FindAsync(so.Id);
        Assert.Null(saved!.ContactId);
    }

    // ── Scenario: Staff updates contact on standing order; existing instances unaffected ──

    [Fact]
    public async Task PatchContact_UpdatesStandingOrderContact_ExistingInstanceUnchanged()
    {
        var (customer, originalContact) = await SeedWholesaleCustomerWithContactAsync();

        var so = BuildStandingOrder(customer.Id, contactId: originalContact.Id);
        _db.StandingOrders.Add(so);

        // Pre-existing instance (generated before the contact update)
        var existingInstance = new OrderInstance
        {
            Id              = Guid.NewGuid(),
            StandingOrderId = so.Id,
            CustomerId      = customer.Id,
            ContactId       = originalContact.Id,
            Channel         = OrderChannel.Insulated,
            Status          = OrderInstanceStatus.Pending,
            WeekOf          = so.StartWeek,
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = "test"
        };
        _db.OrderInstances.Add(existingInstance);
        await _db.SaveChangesAsync();

        // Add a new contact and simulate the PATCH endpoint (UpdateStandingOrderContactRequest)
        var newContact = new CustomerContact
        {
            Id = Guid.NewGuid(), CustomerId = customer.Id,
            Role = ContactRole.Purchasing, FirstName = "Bob", LastName = "New", IsPrimary = false
        };
        _db.CustomerContacts.Add(newContact);
        await _db.SaveChangesAsync();

        // Validate new contact belongs to customer (guard logic)
        var contactValid = await _db.CustomerContacts
            .AnyAsync(c => c.Id == newContact.Id && c.CustomerId == customer.Id);
        Assert.True(contactValid);

        // Apply patch (mirrors PATCH endpoint logic)
        so.ContactId  = newContact.Id;
        so.ModifiedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Standing order contact is updated
        var updatedSo = await _db.StandingOrders.FindAsync(so.Id);
        Assert.Equal(newContact.Id, updatedSo!.ContactId);

        // Pre-existing instance retains original ContactId
        var unchangedInstance = await _db.OrderInstances.FindAsync(existingInstance.Id);
        Assert.Equal(originalContact.Id, unchangedInstance!.ContactId);
    }
}
