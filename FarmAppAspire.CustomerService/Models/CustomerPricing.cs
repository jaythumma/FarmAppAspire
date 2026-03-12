namespace FarmAppAspire.CustomerService.Models;

public class CustomerPricing
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public InsulatedBoxSize BoxSize { get; set; }
    public decimal PricePerLb { get; set; }
    public decimal? ShippingRate { get; set; }
    public int MinQty { get; set; } = 1;
}
