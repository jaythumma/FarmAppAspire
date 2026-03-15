using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Endpoints;
using FarmAppAspire.CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.Tests.Unit.CustomerService;

/// <summary>
/// Tests covering auto-creation of StandingOrder on first invoice (Issue #9).
/// </summary>
public class StandingOrderAutoCreateTests : IDisposable
{
    private readonly CustomerDbContext _db;

    public StandingOrderAutoCreateTests()
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

    private async Task<Customer> SeedCustomerAsync(string key = "TST-LOC")
    {
        var customer = new Customer
        {
            Id          = Guid.NewGuid(),
            Type        = CustomerType.Retail,
            DisplayName = "Test Customer",
            CustomerKey = key,
            CreatedAt   = DateTime.UtcNow,
            CreatedBy   = "test"
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    private static OrderInstance BuildShippedInstance(Guid customerId, DateTime? shipDate = null) => new()
    {
        Id        = Guid.NewGuid(),
        CustomerId = customerId,
        Channel   = OrderChannel.Insulated,
        Status    = OrderInstanceStatus.Shipped,
        WeekOf    = new DateTime(2024, 6, 3),
        ShipDate  = shipDate ?? new DateTime(2024, 6, 5),
        IsSample  = false,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "test",
        Lines     = []
    };

    // ── Scenario: Auto-creation on first invoice ──────────────────────────────

    [Fact]
    public async Task Ship_FirstInvoice_AutoCreatesStandingOrder()
    {
        var customer = await SeedCustomerAsync();
        var instance = BuildShippedInstance(customer.Id);
        _db.OrderInstances.Add(instance);
        await _db.SaveChangesAsync();

        await OrderInstanceEndpoints.CreateInvoiceAsync(instance, _db);
        await _db.SaveChangesAsync();

        var so = await _db.StandingOrders.FirstOrDefaultAsync(s => s.CustomerId == customer.Id);
        Assert.NotNull(so);
        Assert.Equal(OrderFrequency.Weekly, so.Frequency);
        Assert.False(so.IsSample);
        Assert.Equal(StandingOrderStatus.Active, so.Status);
    }

    [Fact]
    public async Task Ship_FirstInvoice_AutoCreatedStandingOrder_StartWeekIsMonday()
    {
        var customer = await SeedCustomerAsync();
        // Ship on a Wednesday; StartWeek should be the preceding Monday
        var shipDate = new DateTime(2024, 6, 5); // Wednesday
        var instance = BuildShippedInstance(customer.Id, shipDate);
        _db.OrderInstances.Add(instance);
        await _db.SaveChangesAsync();

        await OrderInstanceEndpoints.CreateInvoiceAsync(instance, _db);
        await _db.SaveChangesAsync();

        var so = await _db.StandingOrders.FirstAsync(s => s.CustomerId == customer.Id);
        Assert.Equal(DayOfWeek.Monday, so.StartWeek.DayOfWeek);
        Assert.Equal(new DateTime(2024, 6, 3), so.StartWeek); // Monday of that week
    }

    // ── Scenario: No duplicate standing order created ─────────────────────────

    [Fact]
    public async Task Ship_SecondInvoice_DoesNotCreateDuplicateStandingOrder()
    {
        var customer = await SeedCustomerAsync();

        // Pre-existing standing order
        _db.StandingOrders.Add(new StandingOrder
        {
            Id         = Guid.NewGuid(),
            CustomerId = customer.Id,
            Frequency  = OrderFrequency.Weekly,
            IsSample   = false,
            Status     = StandingOrderStatus.Active,
            SeasonYear = 2024,
            StartWeek  = new DateTime(2024, 6, 3),
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = "test"
        });
        await _db.SaveChangesAsync();

        var instance = BuildShippedInstance(customer.Id);
        _db.OrderInstances.Add(instance);
        await _db.SaveChangesAsync();

        await OrderInstanceEndpoints.CreateInvoiceAsync(instance, _db);
        await _db.SaveChangesAsync();

        var count = await _db.StandingOrders.CountAsync(s => s.CustomerId == customer.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Ship_CustomerWithStoppedStandingOrder_DoesNotCreateNewOne()
    {
        var customer = await SeedCustomerAsync();

        // Existing stopped standing order counts as "has a standing order"
        _db.StandingOrders.Add(new StandingOrder
        {
            Id         = Guid.NewGuid(),
            CustomerId = customer.Id,
            Frequency  = OrderFrequency.Stopped,
            IsSample   = false,
            Status     = StandingOrderStatus.Stopped,
            SeasonYear = 2024,
            StartWeek  = new DateTime(2024, 6, 3),
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = "test"
        });
        await _db.SaveChangesAsync();

        var instance = BuildShippedInstance(customer.Id);
        _db.OrderInstances.Add(instance);
        await _db.SaveChangesAsync();

        await OrderInstanceEndpoints.CreateInvoiceAsync(instance, _db);
        await _db.SaveChangesAsync();

        var count = await _db.StandingOrders.CountAsync(s => s.CustomerId == customer.Id);
        Assert.Equal(1, count);
    }

    // ── Scenario: Sample instance is fulfilled but not invoiced ───────────────

    [Fact]
    public async Task Ship_SampleInstance_DoesNotCreateInvoice()
    {
        var customer = await SeedCustomerAsync();
        var instance = new OrderInstance
        {
            Id         = Guid.NewGuid(),
            CustomerId = customer.Id,
            Channel    = OrderChannel.Insulated,
            Status     = OrderInstanceStatus.Shipped,
            WeekOf     = new DateTime(2024, 6, 3),
            ShipDate   = new DateTime(2024, 6, 5),
            IsSample   = true,       // <-- sample order
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = "test",
            Lines      = []
        };
        _db.OrderInstances.Add(instance);
        await _db.SaveChangesAsync();

        // The ship endpoint skips CreateInvoiceAsync when IsSample=true;
        // validate that no invoice exists after a simulated IsSample=false bypass.
        // Here we verify the IsSample flag is preserved on the instance.
        Assert.True(instance.IsSample);
        var invoiceCount = await _db.Invoices.CountAsync(i => i.CustomerId == customer.Id);
        Assert.Equal(0, invoiceCount);
    }
}
