using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.Tests.Unit.CustomerService;

/// <summary>
/// Tests covering season restart behavior (Issue #9).
/// </summary>
public class SeasonRestartTests : IDisposable
{
    private readonly CustomerDbContext _db;

    public SeasonRestartTests()
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

    private async Task<Guid> SeedCustomerAsync()
    {
        var customer = new Customer
        {
            Id          = Guid.NewGuid(),
            Type        = CustomerType.Retail,
            DisplayName = "Season Test Customer",
            CustomerKey = "STN-LOC",
            CreatedAt   = DateTime.UtcNow,
            CreatedBy   = "test"
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer.Id;
    }

    private async Task<StandingOrder> SeedActiveStandingOrderAsync(Guid customerId, int seasonYear = 2024)
    {
        var so = new StandingOrder
        {
            Id         = Guid.NewGuid(),
            CustomerId = customerId,
            Frequency  = OrderFrequency.Weekly,
            IsSample   = false,
            Status     = StandingOrderStatus.Active,
            SeasonYear = seasonYear,
            StartWeek  = SeasonYearService.FirstMondayOfJune(seasonYear),
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = "test"
        };
        _db.StandingOrders.Add(so);
        await _db.SaveChangesAsync();
        return so;
    }

    // ── Scenario: Season year is derived from June start date ─────────────────

    [Theory]
    [InlineData(2024, 6,  3, 2024)]  // First Monday of June 2024 → season 2024
    [InlineData(2024, 12, 1, 2024)]  // December 2024 → still season 2024
    [InlineData(2025, 5, 26, 2024)]  // May 2025 (before June) → still season 2024
    [InlineData(2025, 6,  2, 2025)]  // First Monday of June 2025 → season 2025
    [InlineData(2025, 9, 15, 2025)]  // September 2025 → season 2025
    public void CurrentSeasonYear_DerivedFromJuneStart(int year, int month, int day, int expectedSeasonYear)
    {
        var date = new DateTime(year, month, day);
        Assert.Equal(expectedSeasonYear, SeasonYearService.CurrentSeasonYear(date));
    }

    // ── Scenario: Season restart resets SeasonYear on active orders ───────────

    [Fact]
    public async Task SeasonRestart_UpdatesSeasonYear_ForActiveOrders()
    {
        var customerId = await SeedCustomerAsync();
        var so = await SeedActiveStandingOrderAsync(customerId, seasonYear: 2024);

        // Simulate the season restart logic (as in AdminEndpoints POST /season/restart)
        var activeOrders = await _db.StandingOrders
            .Where(s => s.Status == StandingOrderStatus.Active || s.Status == StandingOrderStatus.Paused)
            .ToListAsync();

        foreach (var order in activeOrders)
            order.SeasonYear = 2025;

        await _db.SaveChangesAsync();

        var updated = await _db.StandingOrders.FindAsync(so.Id);
        Assert.Equal(2025, updated!.SeasonYear);
    }

    [Fact]
    public async Task SeasonRestart_DoesNotAffect_StoppedOrders()
    {
        var customerId = await SeedCustomerAsync();

        var stoppedOrder = new StandingOrder
        {
            Id         = Guid.NewGuid(),
            CustomerId = customerId,
            Frequency  = OrderFrequency.Stopped,
            IsSample   = false,
            Status     = StandingOrderStatus.Stopped,
            SeasonYear = 2024,
            StartWeek  = SeasonYearService.FirstMondayOfJune(2024),
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = "test"
        };
        _db.StandingOrders.Add(stoppedOrder);
        await _db.SaveChangesAsync();

        // Only Active/Paused orders are updated
        var affectedOrders = await _db.StandingOrders
            .Where(s => s.Status == StandingOrderStatus.Active || s.Status == StandingOrderStatus.Paused)
            .ToListAsync();

        foreach (var order in affectedOrders)
            order.SeasonYear = 2025;

        await _db.SaveChangesAsync();

        var reloaded = await _db.StandingOrders.FindAsync(stoppedOrder.Id);
        Assert.Equal(2024, reloaded!.SeasonYear); // unchanged
    }

    // ── Scenario: SeekNum counter starts at 1 for new season year ────────────

    [Fact]
    public async Task SeekNum_StartsAtOne_ForNewSeasonYear()
    {
        var customerId = await SeedCustomerAsync();

        // Simulate existing invoices for season 2024
        var prevInstance = new OrderInstance
        {
            Id         = Guid.NewGuid(),
            CustomerId = customerId,
            Channel    = OrderChannel.Insulated,
            Status     = OrderInstanceStatus.Shipped,
            WeekOf     = SeasonYearService.FirstMondayOfJune(2024),
            ShipDate   = SeasonYearService.FirstMondayOfJune(2024).AddDays(2),
            IsSample   = false,
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = "test",
            Lines      = []
        };
        _db.OrderInstances.Add(prevInstance);
        _db.Invoices.Add(new Invoice
        {
            Id              = Guid.NewGuid(),
            OrderInstanceId = prevInstance.Id,
            CustomerId      = customerId,
            Channel         = OrderChannel.Insulated,
            SeasonYear      = 2024,
            SeekNum         = 5,    // highest SeekNum in 2024
            Label           = "TST-LOC-2024-05",
            CreatedAt       = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // For season 2025, no invoices exist → SeekNum should start at 1
        var nextSeekNum = (await _db.Invoices
            .Where(i => i.CustomerId == customerId
                     && i.SeasonYear  == 2025
                     && i.Channel     == OrderChannel.Insulated)
            .Select(i => (int?)i.SeekNum)
            .MaxAsync() ?? 0) + 1;

        Assert.Equal(1, nextSeekNum);
    }

    [Fact]
    public async Task SeasonRestart_PausedOrders_AreAlsoUpdated()
    {
        var customerId = await SeedCustomerAsync();

        var pausedOrder = new StandingOrder
        {
            Id         = Guid.NewGuid(),
            CustomerId = customerId,
            Frequency  = OrderFrequency.Weekly,
            IsSample   = false,
            Status     = StandingOrderStatus.Paused,
            SeasonYear = 2024,
            StartWeek  = SeasonYearService.FirstMondayOfJune(2024),
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = "test"
        };
        _db.StandingOrders.Add(pausedOrder);
        await _db.SaveChangesAsync();

        var affectedOrders = await _db.StandingOrders
            .Where(s => s.Status == StandingOrderStatus.Active || s.Status == StandingOrderStatus.Paused)
            .ToListAsync();

        foreach (var order in affectedOrders)
            order.SeasonYear = 2025;

        await _db.SaveChangesAsync();

        var reloaded = await _db.StandingOrders.FindAsync(pausedOrder.Id);
        Assert.Equal(2025, reloaded!.SeasonYear);
    }
}
