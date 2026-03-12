namespace FarmAppAspire.CustomerService.Models;

public class StandingOrder
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public Guid? ContactId { get; set; }
    public CustomerContact? Contact { get; set; }

    public StandingOrderStatus Status { get; set; } = StandingOrderStatus.Active;
    public OrderFrequency Frequency { get; set; } = OrderFrequency.Weekly;
    /// <summary>Used when Frequency = Monthly. Specifies which Monday of the month (1–4).</summary>
    public MonthlyWeek? MonthlyWeek { get; set; }
    public bool IsSample { get; set; }
    public int SeasonYear { get; set; }
    public DateTime StartWeek { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    public ICollection<StandingOrderLine> Lines { get; set; } = [];
    public ICollection<StandingOrderSkip> Skips { get; set; } = [];
    public ICollection<OrderInstance> Instances { get; set; } = [];
}

public class StandingOrderLine
{
    public Guid Id { get; set; }
    public Guid StandingOrderId { get; set; }
    public StandingOrder StandingOrder { get; set; } = null!;
    public InsulatedBoxSize BoxSize { get; set; }
    public int Qty { get; set; }
}

public class StandingOrderSkip
{
    public Guid Id { get; set; }
    public Guid StandingOrderId { get; set; }
    public StandingOrder StandingOrder { get; set; } = null!;
    /// <summary>The Monday date of the week to skip.</summary>
    public DateTime WeekOf { get; set; }
    public DateTime CreatedAt { get; set; }
}
