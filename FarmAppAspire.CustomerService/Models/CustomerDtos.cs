using System.ComponentModel.DataAnnotations;

namespace FarmAppAspire.CustomerService.Models;

// ── Summary (list view) ──────────────────────────────────────────────────────
public record CustomerSummaryDto(
    Guid Id, CustomerType Type, ChannelType ChannelType, string DisplayName,
    string? CompanyName, string? PrimaryEmail, string? PrimaryPhone,
    int OrderCount, decimal TotalOrderAmount,
    string? CustomerKey, bool CustomerKeyCollision);

// ── Detail (single customer with children) ───────────────────────────────────
public record CustomerDetailDto(
    Guid Id, CustomerType Type, ChannelType ChannelType, string DisplayName,
    string? CompanyName, string? TaxId, PaymentTerms? PaymentTerms,
    string? PrimaryEmail, string? PrimaryPhone, string? Notes,
    DateTime CreatedAt, string CreatedBy, DateTime? ModifiedAt, string? ModifiedBy,
    bool BillingUsesShipping,
    string? CustomerKey,
    bool CustomerKeyCollision,
    IEnumerable<ContactDto> Contacts,
    IEnumerable<AddressDto> Addresses,
    int OrderCount,
    decimal TotalOrderAmount);

public record ContactDto(
    Guid Id, ContactRole Role, string FirstName, string LastName,
    string? Email, string? Phone, string? Mobile, bool IsPrimary);

public record AddressDto(
    Guid Id, string Label, AddressType Type,
    string Line1, string? Line2, string City, string State,
    string PostalCode, string Country, bool IsDefault);

// ── Embedded address fields (used in create request) ─────────────────────────
public record AddressFields(
    [Required, MinLength(1)] string Line1,
    string? Line2,
    [Required, MinLength(1)] string City,
    [Required, MinLength(1)] string State,
    [Required, MinLength(1)] string PostalCode,
    [Required, MinLength(1)] string Country);

// ── Create / Update requests ─────────────────────────────────────────────────
public record CreateCustomerRequest(
    [Required] CustomerType Type,
    [Required, MinLength(1)] string DisplayName,
    string? CompanyName, string? TaxId, PaymentTerms? PaymentTerms,
    string? PrimaryEmail, string? PrimaryPhone, string? Notes,
    [Required] AddressFields ShippingAddress,
    bool BillingUsesShipping = true,
    AddressFields? BillingAddress = null,
    ChannelType ChannelType = ChannelType.Direct);

public record UpdateCustomerRequest(
    [Required, MinLength(1)] string DisplayName,
    string? CompanyName, string? TaxId, PaymentTerms? PaymentTerms,
    string? PrimaryEmail, string? PrimaryPhone, string? Notes,
    bool? BillingUsesShipping = null,
    ChannelType ChannelType = ChannelType.Direct);

public record CreateContactRequest(
    [Required] ContactRole Role,
    [Required, MinLength(1)] string FirstName,
    [Required, MinLength(1)] string LastName,
    string? Email, string? Phone, string? Mobile, bool IsPrimary);

public record UpdateContactRequest(
    [Required] ContactRole Role,
    [Required, MinLength(1)] string FirstName,
    [Required, MinLength(1)] string LastName,
    string? Email, string? Phone, string? Mobile, bool IsPrimary);

public record CreateAddressRequest(
    [Required, MinLength(1)] string Label,
    [Required] AddressType Type,
    [Required, MinLength(1)] string Line1,
    string? Line2,
    [Required, MinLength(1)] string City,
    [Required, MinLength(1)] string State,
    [Required, MinLength(1)] string PostalCode,
    [Required, MinLength(1)] string Country,
    bool IsDefault = false);

public record UpdateAddressRequest(
    [Required, MinLength(1)] string Label,
    [Required] AddressType Type,
    [Required, MinLength(1)] string Line1,
    string? Line2,
    [Required, MinLength(1)] string City,
    [Required, MinLength(1)] string State,
    [Required, MinLength(1)] string PostalCode,
    [Required, MinLength(1)] string Country,
    bool IsDefault = false);

// ── Mapping helpers ───────────────────────────────────────────────────────────
public static class CustomerMappings
{
    public static CustomerDetailDto ToDetailDto(this Customer c, int orderCount = 0, decimal totalOrderAmount = 0m) => new(
        c.Id, c.Type, c.ChannelType, c.DisplayName, c.CompanyName, c.TaxId, c.PaymentTerms,
        c.PrimaryEmail, c.PrimaryPhone, c.Notes,
        c.CreatedAt, c.CreatedBy, c.ModifiedAt, c.ModifiedBy,
        c.BillingUsesShipping,
        c.CustomerKey,
        c.CustomerKeyCollision,
        c.Contacts.Select(x => x.ToDto()).OrderByDescending(x => x.IsPrimary),
        c.Addresses.Select(x => x.ToDto()).OrderByDescending(x => x.IsDefault),
        orderCount,
        totalOrderAmount);

    public static CustomerSummaryDto ToSummaryDto(this Customer c, int orderCount = 0, decimal totalOrderAmount = 0m) => new(
        c.Id, c.Type, c.ChannelType, c.DisplayName, c.CompanyName, c.PrimaryEmail, c.PrimaryPhone,
        orderCount, totalOrderAmount, c.CustomerKey, c.CustomerKeyCollision);

    public static ContactDto ToDto(this CustomerContact c) => new(
        c.Id, c.Role, c.FirstName, c.LastName, c.Email, c.Phone, c.Mobile, c.IsPrimary);

    public static AddressDto ToDto(this CustomerAddress a) => new(
        a.Id, a.Label, a.Type, a.Line1, a.Line2, a.City, a.State,
        a.PostalCode, a.Country, a.IsDefault);
}
