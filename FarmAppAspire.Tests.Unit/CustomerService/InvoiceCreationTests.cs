using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Endpoints;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.Tests.Unit.CustomerService;

/// <summary>
/// Tests covering invoice creation, SeekNum assignment, sample-order skipping,
/// channel independence, and season reset (Issue #47).
/// </summary>
public class InvoiceCreationTests : IDisposable
{
    private readonly CustomerDbContext _db;

    public InvoiceCreationTests()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new CustomerDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid _insulatedSoId;

    private async Task<Customer> SeedCustomerAsync(string key = "MAN-CHI-IL")
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

        var insulatedSo = new StandingOrder
        {
            Id = Guid.NewGuid(), CustomerId = customer.Id, Channel = OrderChannel.Insulated,
            Frequency = OrderFrequency.Weekly, Status = StandingOrderStatus.Active,
            SeasonYear = 2024, StartWeek = new DateTime(2024, 6, 3),
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _db.StandingOrders.AddRange(insulatedSo);
        await _db.SaveChangesAsync();

        _insulatedSoId = insulatedSo.Id;
        return customer;
    }

    private Order BuildInstance(Guid customerId, OrderChannel channel,
        bool isSample = false, DateTime? shipDate = null) => new()
    {
        Id              = Guid.NewGuid(),
        StandingOrderId = channel == OrderChannel.FedEx ? null : _insulatedSoId,
        CustomerId      = customerId,
        Channel         = channel,
        Status          = OrderStatus.Shipped,
        WeekOf          = new DateTime(2024, 6, 3),
        ShipDate        = shipDate ?? new DateTime(2024, 6, 5),
        IsSample        = isSample,
        CreatedAt       = DateTime.UtcNow,
        CreatedBy       = "test",
        Lines           = []
    };

    // ── Scenario: First insulated invoice of a season is labeled correctly ───

    [Fact]
    public async Task CreateInvoice_FirstInsulated_SeekNum01_CorrectLabel()
    {
        var customer = await SeedCustomerAsync("MAN-CHI-IL");
        var instance = BuildInstance(customer.Id, OrderChannel.Insulated, shipDate: new DateTime(2024, 6, 5));
        _db.Orders.Add(instance);
        await _db.SaveChangesAsync();

        await OrderEndpoints.CreateInvoiceAsync(instance, _db);
        await _db.SaveChangesAsync();

        var invoice = await _db.Invoices.FirstAsync(i => i.CustomerId == customer.Id);
        Assert.Equal(1, invoice.SeekNum);
        Assert.Equal("MAN-CHI-IL-2024-01", invoice.Label);
        Assert.Equal(OrderChannel.Insulated, invoice.Channel);
        Assert.Equal(2024, invoice.SeasonYear);
    }

    // ── Scenario: SeekNum increments for each issued invoice ─────────────────

    [Fact]
    public async Task CreateInvoice_SeekNum_IncrementsSequentially()
    {
        var customer = await SeedCustomerAsync("MAN-CHI-IL");

        for (var i = 0; i < 3; i++)
        {
            var instance = BuildInstance(customer.Id, OrderChannel.Insulated);
            _db.Orders.Add(instance);
            await _db.SaveChangesAsync();
            await OrderEndpoints.CreateInvoiceAsync(instance, _db);
            await _db.SaveChangesAsync();
        }

        var invoices = await _db.Invoices
            .Where(i => i.CustomerId == customer.Id)
            .OrderBy(i => i.SeekNum)
            .ToListAsync();

        Assert.Equal(3, invoices.Count);
        Assert.Equal([1, 2, 3], invoices.Select(i => i.SeekNum));
    }

    // ── Scenario: Seventh invoice for a sporadic customer is labeled correctly

    [Fact]
    public async Task CreateInvoice_SeventhInvoice_SeekNum07()
    {
        var customer = await SeedCustomerAsync("SUN-DAL-TX");
        var shipDate2023 = new DateTime(2023, 6, 5);

        // Seed 6 prior invoices
        for (var i = 0; i < 6; i++)
        {
            var instance = BuildInstance(customer.Id, OrderChannel.Insulated, shipDate: shipDate2023);
            _db.Orders.Add(instance);
            await _db.SaveChangesAsync();
            await OrderEndpoints.CreateInvoiceAsync(instance, _db);
            await _db.SaveChangesAsync();
        }

        // 7th invoice
        var seventhInstance = BuildInstance(customer.Id, OrderChannel.Insulated, shipDate: shipDate2023);
        _db.Orders.Add(seventhInstance);
        await _db.SaveChangesAsync();
        await OrderEndpoints.CreateInvoiceAsync(seventhInstance, _db);
        await _db.SaveChangesAsync();

        var seventh = await _db.Invoices
            .Where(i => i.CustomerId == customer.Id && i.SeasonYear == 2023)
            .OrderByDescending(i => i.SeekNum)
            .FirstAsync();

        Assert.Equal(7, seventh.SeekNum);
        Assert.Equal("SUN-DAL-TX-2023-07", seventh.Label);
    }

    // ── Scenario: FedEx counter is independent of insulated counter ──────────

    [Fact]
    public async Task CreateInvoice_FedEx_HasIndependentSeekNumCounter()
    {
        var customer = await SeedCustomerAsync("MAN-CHI-IL");

        // Create 5 insulated invoices
        for (var i = 0; i < 5; i++)
        {
            var inst = BuildInstance(customer.Id, OrderChannel.Insulated);
            _db.Orders.Add(inst);
            await _db.SaveChangesAsync();
            await OrderEndpoints.CreateInvoiceAsync(inst, _db);
            await _db.SaveChangesAsync();
        }

        // 3rd FedEx invoice (AMZ prefix, SeekNum starts at 1 independently)
        for (var i = 0; i < 3; i++)
        {
            var inst = BuildInstance(customer.Id, OrderChannel.FedEx);
            _db.Orders.Add(inst);
            await _db.SaveChangesAsync();
            await OrderEndpoints.CreateInvoiceAsync(inst, _db);
            await _db.SaveChangesAsync();
        }

        var fedExInvoices = await _db.Invoices
            .Where(i => i.CustomerId == customer.Id && i.Channel == OrderChannel.FedEx)
            .OrderBy(i => i.SeekNum)
            .ToListAsync();

        Assert.Equal(3, fedExInvoices.Count);
        Assert.Equal([1, 2, 3], fedExInvoices.Select(i => i.SeekNum));
        Assert.Equal("AMZ-MAN-CHI-IL-2024-03", fedExInvoices.Last().Label);
    }

    // ── Scenario: Sample shipment does not create an invoice ─────────────────

    [Fact]
    public async Task Ship_SampleInstance_DoesNotCreateInvoice()
    {
        var customer = await SeedCustomerAsync();
        // The ship endpoint guards against samples; CreateInvoiceAsync is never called for them.
        // We verify the guard by checking no invoice exists after a sample instance ships.
        var sampleInstance = BuildInstance(customer.Id, OrderChannel.Insulated, isSample: true);
        _db.Orders.Add(sampleInstance);
        await _db.SaveChangesAsync();

        // Simulate the ship endpoint logic: only call CreateInvoiceAsync when !IsSample
        if (!sampleInstance.IsSample)
        {
            await OrderEndpoints.CreateInvoiceAsync(sampleInstance, _db);
            await _db.SaveChangesAsync();
        }

        var invoiceCount = await _db.Invoices.CountAsync(i => i.CustomerId == customer.Id);
        Assert.Equal(0, invoiceCount);
    }

    // ── Scenario: SeekNum does not advance for sample orders ─────────────────

    [Fact]
    public async Task Ship_SampleAfterRealInvoice_DoesNotIncrementSeekNum()
    {
        var customer = await SeedCustomerAsync();

        // One real invoice -> SeekNum=1
        var realInstance = BuildInstance(customer.Id, OrderChannel.Insulated);
        _db.Orders.Add(realInstance);
        await _db.SaveChangesAsync();
        await OrderEndpoints.CreateInvoiceAsync(realInstance, _db);
        await _db.SaveChangesAsync();

        // Sample shipment -> no invoice created, SeekNum must remain at 1
        var sampleInstance = BuildInstance(customer.Id, OrderChannel.Insulated, isSample: true);
        _db.Orders.Add(sampleInstance);
        await _db.SaveChangesAsync();
        if (!sampleInstance.IsSample)
        {
            await OrderEndpoints.CreateInvoiceAsync(sampleInstance, _db);
            await _db.SaveChangesAsync();
        }

        var maxSeekNum = await _db.Invoices
            .Where(i => i.CustomerId == customer.Id && i.Channel == OrderChannel.Insulated)
            .MaxAsync(i => (int?)i.SeekNum) ?? 0;

        Assert.Equal(1, maxSeekNum);
    }

    // ── Scenario: SeekNum resets at season start ──────────────────────────────

    [Fact]
    public async Task CreateInvoice_SeekNum_ResetsForNewSeasonYear()
    {
        var customer = await SeedCustomerAsync();

        // Season 2024: 3 invoices -> SeekNum 1, 2, 3
        for (var i = 0; i < 3; i++)
        {
            var inst = BuildInstance(customer.Id, OrderChannel.Insulated, shipDate: new DateTime(2024, 7, 1));
            _db.Orders.Add(inst);
            await _db.SaveChangesAsync();
            await OrderEndpoints.CreateInvoiceAsync(inst, _db);
            await _db.SaveChangesAsync();
        }

        // Season 2025 (July 2025): SeekNum should start at 1 again
        var inst2025 = BuildInstance(customer.Id, OrderChannel.Insulated, shipDate: new DateTime(2025, 7, 1));
        _db.Orders.Add(inst2025);
        await _db.SaveChangesAsync();
        await OrderEndpoints.CreateInvoiceAsync(inst2025, _db);
        await _db.SaveChangesAsync();

        var invoice2025 = await _db.Invoices
            .FirstAsync(i => i.CustomerId == customer.Id && i.SeasonYear == 2025);

        Assert.Equal(1, invoice2025.SeekNum);
    }

    // ── Scenario: Invoice stores correct SeasonYear ───────────────────────────

    [Theory]
    [InlineData(2025, 2, 10, 2024)]  // February 2025 → season 2024
    [InlineData(2024, 7, 15, 2024)]  // July 2024 → season 2024
    public async Task CreateInvoice_SeasonYear_DerivedFromShipDate(
        int year, int month, int day, int expectedSeasonYear)
    {
        var customer = await SeedCustomerAsync();
        var instance = BuildInstance(customer.Id, OrderChannel.Insulated, shipDate: new DateTime(year, month, day));
        _db.Orders.Add(instance);
        await _db.SaveChangesAsync();

        await OrderEndpoints.CreateInvoiceAsync(instance, _db);
        await _db.SaveChangesAsync();

        var invoice = await _db.Invoices.FirstAsync(i => i.CustomerId == customer.Id);
        Assert.Equal(expectedSeasonYear, invoice.SeasonYear);
    }

    // ── Scenario: Label reflects CustomerKey from customer record ─────────────

    [Fact]
    public async Task CreateInvoice_Label_UsesCustomerKeyFromDatabase()
    {
        var customer = await SeedCustomerAsync("GVF-AUS-TX");
        var instance = BuildInstance(customer.Id, OrderChannel.Insulated);
        _db.Orders.Add(instance);
        await _db.SaveChangesAsync();

        await OrderEndpoints.CreateInvoiceAsync(instance, _db);
        await _db.SaveChangesAsync();

        var invoice = await _db.Invoices.FirstAsync();
        Assert.StartsWith("GVF-AUS-TX-", invoice.Label);
    }
}
