namespace FarmAppAspire.CustomerService.Models;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid OrderInstanceId { get; set; }
    public OrderInstance OrderInstance { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public OrderChannel Channel { get; set; }
    public int SeasonYear { get; set; }
    public int SeekNum { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
