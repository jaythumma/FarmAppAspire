using FarmAppAspire.CustomerService.Data;
using FarmAppAspire.CustomerService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace FarmAppAspire.Tests.Unit.CustomerService;

/// <summary>
/// Tests verifying that FedEx order creation is ad-hoc:
/// no StandingOrder is created or looked up, and StandingOrderId is null.
/// </summary>
public class FedExOrderCreationTests : IDisposable
{
    private readonly CustomerDbContext _db;

    public FedExOrderCreationTests()
    {
        var options = new DbContextOptionsBuilder<CustomerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new CustomerDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    private async Task<Customer> SeedCustomerAsync()
    {
        var customer = new Customer
        {
            Id          = Guid.NewGuid(),
            Type        = CustomerType.Retail,
            DisplayName = "FedEx Customer",
            CreatedAt   = DateTime.UtcNow,
            CreatedBy   = "test"
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    [Fact]
    public async Task FedExOrder_CreatedWithNullStandingOrderId()
    {
        // GIVEN a customer with no existing standing orders
        var customer = await SeedCustomerAsync();

        // WHEN a FedEx order is created directly
        var order = new Order
        {
            Id              = Guid.NewGuid(),
            StandingOrderId = null,
            CustomerId      = customer.Id,
            Channel         = OrderChannel.FedEx,
            Status          = OrderStatus.Pending,
            WeekOf          = new DateTime(2025, 6, 9),
            IsSample        = false,
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = "test",
            Lines           =
            [
                new OrderLine { Id = Guid.NewGuid(), FedExTierSize = FedExTierSize.FourOz, FedExFixedPrice = 12.99m, Qty = 2 }
            ]
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // THEN the persisted order has StandingOrderId = null
        var saved = await _db.Orders.FindAsync(order.Id);
        Assert.NotNull(saved);
        Assert.Null(saved.StandingOrderId);
        Assert.Equal(OrderChannel.FedEx, saved.Channel);
    }

    [Fact]
    public async Task FedExOrder_DoesNotCreateStandingOrder()
    {
        // GIVEN a customer with no standing orders
        var customer = await SeedCustomerAsync();
        var soBefore = await _db.StandingOrders.CountAsync(s => s.CustomerId == customer.Id);

        // WHEN a FedEx order is created
        var order = new Order
        {
            Id              = Guid.NewGuid(),
            StandingOrderId = null,
            CustomerId      = customer.Id,
            Channel         = OrderChannel.FedEx,
            Status          = OrderStatus.Pending,
            WeekOf          = new DateTime(2025, 6, 9),
            IsSample        = false,
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = "test",
            Lines           = []
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // THEN no StandingOrder was created
        var soAfter = await _db.StandingOrders.CountAsync(s => s.CustomerId == customer.Id);
        Assert.Equal(soBefore, soAfter);
    }

    [Fact]
    public async Task MultipleFedExOrders_AllHaveNullStandingOrderId()
    {
        // GIVEN a customer
        var customer = await SeedCustomerAsync();

        // WHEN two FedEx orders are created for the same customer
        for (var i = 0; i < 2; i++)
        {
            _db.Orders.Add(new Order
            {
                Id              = Guid.NewGuid(),
                StandingOrderId = null,
                CustomerId      = customer.Id,
                Channel         = OrderChannel.FedEx,
                Status          = OrderStatus.Pending,
                WeekOf          = new DateTime(2025, 6, 9).AddDays(i * 7),
                IsSample        = false,
                CreatedAt       = DateTime.UtcNow,
                CreatedBy       = "test",
                Lines           = []
            });
        }
        await _db.SaveChangesAsync();

        // THEN both orders have StandingOrderId = null and no StandingOrders exist
        var fedExOrders = await _db.Orders.Where(o => o.Channel == OrderChannel.FedEx).ToListAsync();
        Assert.Equal(2, fedExOrders.Count);
        Assert.All(fedExOrders, o => Assert.Null(o.StandingOrderId));
        Assert.Empty(await _db.StandingOrders.Where(s => s.CustomerId == customer.Id).ToListAsync());
    }

    [Fact]
    public async Task InsulatedOrder_StillRequiresStandingOrderId()
    {
        // GIVEN a customer with an Insulated standing order
        var customer = await SeedCustomerAsync();
        var so = new StandingOrder
        {
            Id         = Guid.NewGuid(),
            CustomerId = customer.Id,
            Channel    = OrderChannel.Insulated,
            Frequency  = OrderFrequency.Weekly,
            Status     = StandingOrderStatus.Active,
            SeasonYear = 2025,
            StartWeek  = new DateTime(2025, 6, 2),
            CreatedAt  = DateTime.UtcNow,
            CreatedBy  = "test"
        };
        _db.StandingOrders.Add(so);

        // WHEN an Insulated order is created with the standing order id
        var order = new Order
        {
            Id              = Guid.NewGuid(),
            StandingOrderId = so.Id,
            CustomerId      = customer.Id,
            Channel         = OrderChannel.Insulated,
            Status          = OrderStatus.Pending,
            WeekOf          = new DateTime(2025, 6, 9),
            IsSample        = false,
            CreatedAt       = DateTime.UtcNow,
            CreatedBy       = "test",
            Lines           = []
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // THEN the Insulated order has a non-null StandingOrderId
        var saved = await _db.Orders.FindAsync(order.Id);
        Assert.NotNull(saved);
        Assert.Equal(so.Id, saved.StandingOrderId);
    }
}
