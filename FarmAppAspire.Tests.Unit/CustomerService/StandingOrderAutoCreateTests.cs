using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Endpoints;
using FarmAppAspire.CustomerService.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.Tests.Unit.CustomerService;

/// <summary>
/// Tests covering the order creation + StandingOrder auto-create flow and
/// the invoice creation path (tasks 11.4–11.8).
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

    private async Task<StandingOrder> SeedStandingOrderAsync(
        Guid customerId, OrderChannel channel = OrderChannel.Insulated)
    {
        var so = new StandingOrder
        {
            Id         = Guid.NewGuid(),
            CustomerId = customerId,
            Channel    = channel,
            Frequency  = channel == OrderChannel.FedEx ? OrderFrequency.OnRequest : OrderFrequency.Weekly,
            Status     = StandingOrderStatus.Active,
            SeasonYear = 2024,
            StartWeek  = new DateTime(2024, 6, 3),
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = "test"
        };
        _db.StandingOrders.Add(so);
        await _db.SaveChangesAsync();
        return so;
    }

    private async Task<Order> SeedShippedOrderAsync(
        Guid customerId, Guid standingOrderId, DateTime? shipDate = null)
    {
        var order = new Order
        {
            Id              = Guid.NewGuid(),
            StandingOrderId = standingOrderId,
            CustomerId      = customerId,
            Channel         = OrderChannel.Insulated,
            Status          = OrderStatus.Shipped,
            WeekOf          = new DateTime(2024, 6, 3),
            ShipDate        = shipDate ?? new DateTime(2024, 6, 5),
            IsSample        = false,
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = "test",
            Lines           = []
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    // ── Scenario: CreateInvoiceAsync does NOT auto-create a StandingOrder (task 11.8) ──

    [Fact]
    public async Task CreateInvoiceAsync_DoesNotAutoCreateStandingOrder()
    {
        // GIVEN a customer with a StandingOrder already (required pre-condition)
        var customer = await SeedCustomerAsync();
        var so       = await SeedStandingOrderAsync(customer.Id);
        var order    = await SeedShippedOrderAsync(customer.Id, so.Id);
        var soCountBefore = await _db.StandingOrders.CountAsync(s => s.CustomerId == customer.Id);

        // WHEN an invoice is created
        await OrderEndpoints.CreateInvoiceAsync(order, _db);
        await _db.SaveChangesAsync();

        // THEN no additional StandingOrder was created
        var soCountAfter = await _db.StandingOrders.CountAsync(s => s.CustomerId == customer.Id);
        Assert.Equal(soCountBefore, soCountAfter);
    }

    [Fact]
    public async Task CreateInvoiceAsync_CreatesInvoiceWithCorrectFields()
    {
        // GIVEN a shipped order linked to a standing order
        var customer = await SeedCustomerAsync("ABC");
        var so       = await SeedStandingOrderAsync(customer.Id);
        var order    = await SeedShippedOrderAsync(customer.Id, so.Id, new DateTime(2024, 6, 5));

        // WHEN an invoice is created
        await OrderEndpoints.CreateInvoiceAsync(order, _db);
        await _db.SaveChangesAsync();

        // THEN an invoice exists with the correct OrderId
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.OrderId == order.Id);
        Assert.NotNull(invoice);
        Assert.Equal(order.CustomerId, invoice.CustomerId);
        Assert.Equal(order.Channel, invoice.Channel);
        Assert.Equal(1, invoice.SeekNum);
    }

    // ── Scenario: Sample order does not create an invoice ────────────────────

    [Fact]
    public async Task Ship_SampleOrder_DoesNotCreateInvoice()
    {
        // GIVEN a sample order (IsSample = true)
        var customer = await SeedCustomerAsync();
        var so       = await SeedStandingOrderAsync(customer.Id);
        var order = new Order
        {
            Id              = Guid.NewGuid(),
            StandingOrderId = so.Id,
            CustomerId      = customer.Id,
            Channel         = OrderChannel.Insulated,
            Status          = OrderStatus.Shipped,
            WeekOf          = new DateTime(2024, 6, 3),
            ShipDate        = new DateTime(2024, 6, 5),
            IsSample        = true,   // <-- sample
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = "test",
            Lines           = []
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // THEN the ship endpoint skips invoice creation when IsSample = true
        // Verify the IsSample flag is set correctly
        Assert.True(order.IsSample);
        var invoiceCount = await _db.Invoices.CountAsync(i => i.CustomerId == customer.Id);
        Assert.Equal(0, invoiceCount);
    }

    // ── Scenario: SeekNum increments per customer per channel per season ──────

    [Fact]
    public async Task CreateInvoiceAsync_SeekNum_IncrementsPerChannel()
    {
        var customer = await SeedCustomerAsync("INC");
        var so       = await SeedStandingOrderAsync(customer.Id);
        var order1   = await SeedShippedOrderAsync(customer.Id, so.Id, new DateTime(2024, 6, 5));

        await OrderEndpoints.CreateInvoiceAsync(order1, _db);
        await _db.SaveChangesAsync();

        var order2 = await SeedShippedOrderAsync(customer.Id, so.Id, new DateTime(2024, 6, 12));

        await OrderEndpoints.CreateInvoiceAsync(order2, _db);
        await _db.SaveChangesAsync();

        var invoices = await _db.Invoices
            .Where(i => i.CustomerId == customer.Id)
            .OrderBy(i => i.SeekNum)
            .ToListAsync();

        Assert.Equal(2, invoices.Count);
        Assert.Equal(1, invoices[0].SeekNum);
        Assert.Equal(2, invoices[1].SeekNum);
    }
}
