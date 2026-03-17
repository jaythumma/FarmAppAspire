namespace FarmAppAspire.CustomerService.Models;

public class Order
{
    public Guid Id { get; set; }
    public Guid? StandingOrderId { get; set; }
    public StandingOrder? StandingOrder { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid? ContactId { get; set; }
    public CustomerContact? Contact { get; set; }

    public OrderChannel Channel { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    /// <summary>For insulated: the Monday of the harvest week. For FedEx: the order date.</summary>
    public DateTime WeekOf { get; set; }
    public DateTime? ShipDate { get; set; }
    public bool IsSample { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = [];
    public Invoice? Invoice { get; set; }
}

public class OrderLine
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    // Insulated fields
    public InsulatedBoxSize? BoxSize { get; set; }
    public decimal? EffectivePricePerLb { get; set; }

    // FedEx fields
    public FedExTierSize? FedExTierSize { get; set; }
    public decimal? FedExFixedPrice { get; set; }

    public int Qty { get; set; }
    public FedExPackagingType? PackagingType { get; set; }
}
