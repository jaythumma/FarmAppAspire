using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.Tests.Unit.CustomerService;

/// <summary>
/// Tests for the admin browse-orders endpoint logic, specifically verifying that
/// customer display names are populated and the UTC date range query works correctly.
/// </summary>
public class AdminBrowseOrdersTests : IDisposable
{
    private readonly CustomerDbContext _db;

    public AdminBrowseOrdersTests()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new CustomerDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    // ── Customer display name is populated from DB lookup ─────────────────────

    [Fact]
    public async Task BrowseOrders_ReturnsCustomerDisplayName_WhenCustomerExists()
    {
        // GIVEN a customer and an order for the week of 2025-04-07
        var weekOf     = new DateTime(2025, 4, 7, 0, 0, 0, DateTimeKind.Utc);
        var customerId = Guid.NewGuid();

        _db.Customers.Add(new Customer
        {
            Id = customerId, Type = CustomerType.Retail, DisplayName = "Green Valley Farm",
            CustomerKey = "GVF-001", CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });

        var standingOrder = new StandingOrder
        {
            Id = Guid.NewGuid(), CustomerId = customerId, Channel = OrderChannel.Insulated,
            Frequency = OrderFrequency.Weekly, Status = StandingOrderStatus.Active,
            SeasonYear = 2025, StartWeek = weekOf, CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _db.StandingOrders.Add(standingOrder);

        _db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(), StandingOrderId = standingOrder.Id, CustomerId = customerId,
            Channel = OrderChannel.Insulated, Status = OrderStatus.Pending, WeekOf = weekOf,
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });
        await _db.SaveChangesAsync();

        // WHEN we run the browse query for that week (simulating the endpoint logic)
        var weekStart = DateTime.SpecifyKind(SeasonYearService.MondayOf(weekOf).Date, DateTimeKind.Utc);
        var weekEnd   = weekStart.AddDays(1);

        var orders = await _db.Orders
            .Include(o => o.Lines)
            .Where(o => o.WeekOf >= weekStart && o.WeekOf < weekEnd)
            .ToListAsync();

        var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
        var customers   = await _db.Customers
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.DisplayName, c.CustomerKey })
            .ToListAsync();
        var customerMap = customers.ToDictionary(c => c.Id);

        // THEN the customer display name and key are resolved
        Assert.Single(orders);
        var cust = customerMap.GetValueOrDefault(orders[0].CustomerId);
        Assert.NotNull(cust);
        Assert.Equal("Green Valley Farm", cust.DisplayName);
        Assert.Equal("GVF-001", cust.CustomerKey);
    }

    [Fact]
    public async Task BrowseOrders_ReturnsEmptyList_WhenNoOrdersExistForWeek()
    {
        // GIVEN no orders in the database
        // WHEN we browse for the current week
        var weekStart = DateTime.SpecifyKind(new DateTime(2025, 4, 7), DateTimeKind.Utc);
        var weekEnd   = weekStart.AddDays(1);

        var orders = await _db.Orders
            .Include(o => o.Lines)
            .Where(o => o.WeekOf >= weekStart && o.WeekOf < weekEnd)
            .ToListAsync();

        // THEN no orders are returned
        Assert.Empty(orders);
    }

    [Fact]
    public async Task BrowseOrders_DoesNotReturnOrdersFromDifferentWeek()
    {
        // GIVEN a customer with orders in different weeks
        var customerId = Guid.NewGuid();
        _db.Customers.Add(new Customer
        {
            Id = customerId, Type = CustomerType.Retail, DisplayName = "Sun Ridge Farm",
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });

        var standingOrder = new StandingOrder
        {
            Id = Guid.NewGuid(), CustomerId = customerId, Channel = OrderChannel.Insulated,
            Frequency = OrderFrequency.Weekly, Status = StandingOrderStatus.Active,
            SeasonYear = 2025, StartWeek = new DateTime(2025, 4, 7, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _db.StandingOrders.Add(standingOrder);

        // Order in week of Apr 7
        _db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(), StandingOrderId = standingOrder.Id, CustomerId = customerId,
            Channel = OrderChannel.Insulated, Status = OrderStatus.Pending,
            WeekOf = new DateTime(2025, 4, 7, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });

        // Order in week of Apr 14
        _db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(), StandingOrderId = standingOrder.Id, CustomerId = customerId,
            Channel = OrderChannel.Insulated, Status = OrderStatus.Pending,
            WeekOf = new DateTime(2025, 4, 14, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });
        await _db.SaveChangesAsync();

        // WHEN we browse for week of Apr 7
        var weekStart = new DateTime(2025, 4, 7, 0, 0, 0, DateTimeKind.Utc);
        var weekEnd   = weekStart.AddDays(1);

        var orders = await _db.Orders
            .Include(o => o.Lines)
            .Where(o => o.WeekOf >= weekStart && o.WeekOf < weekEnd)
            .ToListAsync();

        // THEN only the Apr 7 order is returned
        Assert.Single(orders);
        Assert.Equal(weekStart, orders[0].WeekOf);
    }

    [Fact]
    public async Task BrowseOrders_ShowsMultipleCustomers_SortedByDisplayName()
    {
        // GIVEN two customers each with an order in the same week
        var weekOf = new DateTime(2025, 4, 7, 0, 0, 0, DateTimeKind.Utc);

        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();
        _db.Customers.AddRange(
            new Customer
            {
                Id = customerId1, Type = CustomerType.Retail, DisplayName = "Zephyr Acres",
                CreatedAt = DateTime.UtcNow, CreatedBy = "test"
            },
            new Customer
            {
                Id = customerId2, Type = CustomerType.Retail, DisplayName = "Apple Tree Farm",
                CreatedAt = DateTime.UtcNow, CreatedBy = "test"
            }
        );

        var so1 = new StandingOrder
        {
            Id = Guid.NewGuid(), CustomerId = customerId1, Channel = OrderChannel.Insulated,
            Frequency = OrderFrequency.Weekly, Status = StandingOrderStatus.Active,
            SeasonYear = 2025, StartWeek = weekOf, CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        var so2 = new StandingOrder
        {
            Id = Guid.NewGuid(), CustomerId = customerId2, Channel = OrderChannel.Insulated,
            Frequency = OrderFrequency.Weekly, Status = StandingOrderStatus.Active,
            SeasonYear = 2025, StartWeek = weekOf, CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _db.StandingOrders.AddRange(so1, so2);

        _db.Orders.AddRange(
            new Order
            {
                Id = Guid.NewGuid(), StandingOrderId = so1.Id, CustomerId = customerId1,
                Channel = OrderChannel.Insulated, Status = OrderStatus.Pending, WeekOf = weekOf,
                CreatedAt = DateTime.UtcNow, CreatedBy = "test"
            },
            new Order
            {
                Id = Guid.NewGuid(), StandingOrderId = so2.Id, CustomerId = customerId2,
                Channel = OrderChannel.Insulated, Status = OrderStatus.Pending, WeekOf = weekOf,
                CreatedAt = DateTime.UtcNow, CreatedBy = "test"
            }
        );
        await _db.SaveChangesAsync();

        // WHEN we browse and build the result with customer names, sorted
        var weekStart = new DateTime(2025, 4, 7, 0, 0, 0, DateTimeKind.Utc);
        var weekEnd   = weekStart.AddDays(1);

        var orders = await _db.Orders
            .Include(o => o.Lines)
            .Where(o => o.WeekOf >= weekStart && o.WeekOf < weekEnd)
            .ToListAsync();

        var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
        var customers   = await _db.Customers
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.DisplayName, c.CustomerKey })
            .ToListAsync();
        var customerMap = customers.ToDictionary(c => c.Id);

        var summaryNames = orders
            .Select(o => customerMap.GetValueOrDefault(o.CustomerId)?.DisplayName ?? "Unknown")
            .OrderBy(n => n)
            .ToList();

        // THEN both customer names are resolved and sorted alphabetically
        Assert.Equal(2, summaryNames.Count);
        Assert.Equal("Apple Tree Farm", summaryNames[0]);
        Assert.Equal("Zephyr Acres",    summaryNames[1]);
    }

    // ── UTC DateTime range query covers full day ──────────────────────────────

    [Fact]
    public async Task BrowseOrders_UtcRangeQuery_CoversFullDay()
    {
        // GIVEN a customer and an order at UTC midnight
        var customerId = Guid.NewGuid();
        _db.Customers.Add(new Customer
        {
            Id = customerId, Type = CustomerType.Retail, DisplayName = "Sunrise CSA",
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });

        var standingOrder = new StandingOrder
        {
            Id = Guid.NewGuid(), CustomerId = customerId, Channel = OrderChannel.Insulated,
            Frequency = OrderFrequency.Weekly, Status = StandingOrderStatus.Active,
            SeasonYear = 2025, StartWeek = new DateTime(2025, 4, 7, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        };
        _db.StandingOrders.Add(standingOrder);

        _db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(), StandingOrderId = standingOrder.Id, CustomerId = customerId,
            Channel = OrderChannel.Insulated, Status = OrderStatus.Pending,
            WeekOf = new DateTime(2025, 4, 7, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow, CreatedBy = "test"
        });
        await _db.SaveChangesAsync();

        // WHEN we apply the UTC range query for 2025-04-07
        var weekStart = DateTime.SpecifyKind(new DateTime(2025, 4, 7), DateTimeKind.Utc);
        var weekEnd   = weekStart.AddDays(1);

        var orders = await _db.Orders
            .Where(o => o.WeekOf >= weekStart && o.WeekOf < weekEnd)
            .ToListAsync();

        // THEN the order is found
        Assert.Single(orders);
    }
}
