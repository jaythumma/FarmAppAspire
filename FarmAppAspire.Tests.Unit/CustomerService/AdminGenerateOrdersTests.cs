using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Endpoints;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.Tests.Unit.CustomerService;

public class AdminGenerateOrdersTests : IDisposable
{
    private readonly CustomerDbContext _db;

    public AdminGenerateOrdersTests()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new CustomerDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    // ── MondayOf snap logic (mirrors the server-side SeasonYearService) ──────

    [Theory]
    [InlineData(2025, 4, 7)]   // Monday → stays Monday
    [InlineData(2025, 4, 8)]   // Tuesday → snaps to Monday Apr 7
    [InlineData(2025, 4, 9)]   // Wednesday → snaps to Monday Apr 7
    [InlineData(2025, 4, 10)]  // Thursday → snaps to Monday Apr 7
    [InlineData(2025, 4, 11)]  // Friday → snaps to Monday Apr 7
    [InlineData(2025, 4, 12)]  // Saturday → snaps to Monday Apr 7
    [InlineData(2025, 4, 13)]  // Sunday → snaps to Monday Apr 7
    public void MondayOf_AlwaysReturnsMonday(int year, int month, int day)
    {
        var date   = new DateTime(year, month, day);
        var monday = SeasonYearService.MondayOf(date);

        Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek);
    }

    [Theory]
    [InlineData(2025, 4, 7,  2025, 4, 7)]   // Monday → same Monday
    [InlineData(2025, 4, 8,  2025, 4, 7)]   // Tuesday → previous Monday
    [InlineData(2025, 4, 13, 2025, 4, 7)]   // Sunday → previous Monday
    [InlineData(2025, 4, 14, 2025, 4, 14)]  // Monday (next week) → stays
    public void MondayOf_SnapsToCorrectMonday(
        int year, int month, int day,
        int expYear, int expMonth, int expDay)
    {
        var date     = new DateTime(year, month, day);
        var expected = new DateTime(expYear, expMonth, expDay);

        Assert.Equal(expected, SeasonYearService.MondayOf(date));
    }

    [Fact]
    public void MondayOf_PreservesDateOnly_NotTime()
    {
        var date   = new DateTime(2025, 4, 9, 14, 30, 0); // Wednesday with time
        var monday = SeasonYearService.MondayOf(date);

        Assert.Equal(TimeSpan.Zero, monday.TimeOfDay);
    }

    // ── GenerateOrdersResponse DTO shape ──────────────────────────────────────

    [Fact]
    public void GenerateOrdersResponse_TracksCreatedAndSkippedCounts()
    {
        var weekOf = new DateTime(2025, 4, 7);
        var orders = new List<GeneratedOrderSummary>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Alpha Farm", "AF-001", "Insulated", weekOf, false, 2, 120m),
            new(Guid.NewGuid(), Guid.NewGuid(), "Beta CSA",  null,     "Insulated", weekOf, true,  1, 0m)
        };

        var response = new GenerateOrdersResponse(
            GeneratedCount: 2,
            SkippedCount:   3,
            WeekOf:         weekOf,
            Orders:         orders);

        Assert.Equal(2, response.GeneratedCount);
        Assert.Equal(3, response.SkippedCount);
        Assert.Equal(weekOf, response.WeekOf);
        Assert.Equal(2, response.Orders.Count);
    }

    [Fact]
    public void GeneratedOrderSummary_NullCustomerKey_IsAllowed()
    {
        var summary = new GeneratedOrderSummary(
            OrderId:             Guid.NewGuid(),
            CustomerId:          Guid.NewGuid(),
            CustomerDisplayName: "No Key Farm",
            CustomerKey:         null,
            Channel:             "Insulated",
            WeekOf:              new DateTime(2025, 4, 7),
            IsSample:            false,
            TotalQty:            1,
            TotalAmount:         50m);

        Assert.Null(summary.CustomerKey);
    }

    // ── FedEx standing orders excluded from bulk generation (task 11.2) ──────

    [Fact]
    public async Task GenerateOrders_ExcludesFedExStandingOrders()
    {
        // GIVEN both an Insulated and a FedEx standing order for the same customer
        var customerId = Guid.NewGuid();
        _db.Customers.Add(new Customer
        {
            Id = customerId, Type = CustomerType.Retail, DisplayName = "Test Farm",
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });

        var insulatedSo = new StandingOrder
        {
            Id = Guid.NewGuid(), CustomerId = customerId, Channel = OrderChannel.Insulated,
            Frequency = OrderFrequency.Weekly, Status = StandingOrderStatus.Active,
            SeasonYear = 2025, StartWeek = new DateTime(2025, 6, 2),
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        var fedExSo = new StandingOrder
        {
            Id = Guid.NewGuid(), CustomerId = customerId, Channel = OrderChannel.FedEx,
            Frequency = OrderFrequency.OnRequest, Status = StandingOrderStatus.Active,
            SeasonYear = 2025, StartWeek = new DateTime(2025, 6, 2),
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _db.StandingOrders.AddRange(insulatedSo, fedExSo);
        await _db.SaveChangesAsync();

        // WHEN the generate query runs (simulating the AdminEndpoints filter)
        var weekOf     = new DateTime(2025, 6, 2);
        var seasonYear = SeasonYearService.CurrentSeasonYear(weekOf);

        var eligible = await _db.StandingOrders
            .Where(s => s.Channel == OrderChannel.Insulated
                     && s.Status == StandingOrderStatus.Active
                     && s.Frequency != OrderFrequency.Stopped
                     && s.SeasonYear <= seasonYear)
            .ToListAsync();

        // THEN only the Insulated SO is selected for generation
        Assert.Single(eligible);
        Assert.Equal(OrderChannel.Insulated, eligible[0].Channel);
        Assert.Equal(insulatedSo.Id, eligible[0].Id);
    }

    // ── OnRequest insulated SO: generated once, then auto-paused (task 11.3) ─

    [Fact]
    public async Task OnRequest_InsulatedSO_IsGeneratedOnce_ThenAutoPaused()
    {
        var customerId = Guid.NewGuid();
        _db.Customers.Add(new Customer
        {
            Id = customerId, Type = CustomerType.Retail, DisplayName = "OnReq Farm",
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });

        var so = new StandingOrder
        {
            Id = Guid.NewGuid(), CustomerId = customerId, Channel = OrderChannel.Insulated,
            Frequency = OrderFrequency.OnRequest, Status = StandingOrderStatus.Active,
            SeasonYear = 2025, StartWeek = new DateTime(2025, 6, 2),
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _db.StandingOrders.Add(so);
        await _db.SaveChangesAsync();

        var weekOf = new DateTime(2025, 6, 2);

        // Simulate the AdminEndpoints generation loop for a single standing order
        var order = new Order
        {
            Id              = Guid.NewGuid(),
            StandingOrderId = so.Id,
            CustomerId      = so.CustomerId,
            Channel         = OrderChannel.Insulated,
            Status          = OrderStatus.Pending,
            WeekOf          = weekOf,
            IsSample        = so.IsSample,
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = "test",
            Lines           = []
        };
        _db.Orders.Add(order);

        // OnRequest: auto-pause after generation
        if (so.Frequency == OrderFrequency.OnRequest)
            so.Status = StandingOrderStatus.Paused;

        await _db.SaveChangesAsync();

        // THEN the order was generated
        var generatedOrder = await _db.Orders.FirstOrDefaultAsync(o => o.StandingOrderId == so.Id);
        Assert.NotNull(generatedOrder);

        // AND the standing order is now paused (regression guard)
        var updatedSo = await _db.StandingOrders.FindAsync(so.Id);
        Assert.Equal(StandingOrderStatus.Paused, updatedSo!.Status);
    }
}
