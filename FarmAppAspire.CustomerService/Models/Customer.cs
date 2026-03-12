namespace FarmAppAspire.CustomerService.Models;

public class Customer
{
    public Guid Id { get; set; }
    public CustomerType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }

    // Wholesale only
    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }
    public PaymentTerms? PaymentTerms { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    public ICollection<CustomerContact> Contacts { get; set; } = [];
    public ICollection<CustomerAddress> Addresses { get; set; } = [];
}
