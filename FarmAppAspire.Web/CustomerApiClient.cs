using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FarmAppAspire.Web;

public class CustomerApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    public async Task<CustomerSummary[]> GetCustomersAsync(
        int page = 1, int size = 100, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<PagedResult<CustomerSummary>>(
            $"/customers?page={page}&size={size}", JsonOptions, cancellationToken);
        return response?.Items ?? [];
    }

    public async Task<CustomerDetail?> GetCustomerAsync(Guid id, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<CustomerDetail>($"/customers/{id}", JsonOptions, cancellationToken);

    public async Task<CustomerDetail?> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/customers", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerDetail>(JsonOptions, cancellationToken);
    }

    public async Task<CustomerDetail?> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/customers/{id}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerDetail>(JsonOptions, cancellationToken);
    }

    public async Task<bool> DeleteCustomerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/customers/{id}", cancellationToken);
        return response.StatusCode == HttpStatusCode.NoContent;
    }

    public async Task<AddressDto?> CreateAddressAsync(Guid customerId, CreateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/customers/{customerId}/addresses", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AddressDto>(JsonOptions, cancellationToken);
    }

    public async Task<AddressDto?> UpdateAddressAsync(Guid customerId, Guid addressId, UpdateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/customers/{customerId}/addresses/{addressId}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AddressDto>(JsonOptions, cancellationToken);
    }
}

public enum CustomerType { Wholesale, Retail }
public enum ChannelType { Direct, Amazon }
public enum PaymentTerms { NET30, NET60, COD, Prepaid, Other }
public enum ContactRole { Primary, Billing, Purchasing, Shipping, Secondary, Other }
public enum AddressType { Billing, Shipping, Both }

public record PagedResult<T>(int Total, int Page, int Size, T[] Items);

public record CustomerSummary(Guid Id, CustomerType Type, ChannelType ChannelType, string DisplayName, string? CompanyName, string? PrimaryEmail, string? PrimaryPhone, int OrderCount = 0, decimal TotalOrderAmount = 0m);

public record CustomerDetail(
    Guid Id, CustomerType Type, ChannelType ChannelType, string DisplayName,
    string? CompanyName, string? TaxId, PaymentTerms? PaymentTerms,
    string? PrimaryEmail, string? PrimaryPhone, string? Notes,
    DateTime CreatedAt, string CreatedBy, DateTime? ModifiedAt, string? ModifiedBy,
    bool BillingUsesShipping,
    ContactDto[] Contacts,
    AddressDto[] Addresses,
    int OrderCount = 0,
    decimal TotalOrderAmount = 0m);

public record ContactDto(
    Guid Id, ContactRole Role, string FirstName, string LastName,
    string? Email, string? Phone, string? Mobile, bool IsPrimary);

public record AddressDto(
    Guid Id, string Label, AddressType Type,
    string Line1, string? Line2, string City, string State,
    string PostalCode, string Country, bool IsDefault);

public record AddressFields(string Line1, string? Line2, string City, string State, string PostalCode, string Country);

public record CreateCustomerRequest(
    CustomerType Type,
    string DisplayName,
    string? CompanyName,
    string? TaxId,
    PaymentTerms? PaymentTerms,
    string? PrimaryEmail,
    string? PrimaryPhone,
    string? Notes,
    AddressFields ShippingAddress,
    bool BillingUsesShipping = true,
    AddressFields? BillingAddress = null,
    ChannelType ChannelType = ChannelType.Direct);

public record UpdateCustomerRequest(
    string DisplayName,
    string? CompanyName,
    string? TaxId,
    PaymentTerms? PaymentTerms,
    string? PrimaryEmail,
    string? PrimaryPhone,
    string? Notes,
    bool? BillingUsesShipping = null,
    ChannelType ChannelType = ChannelType.Direct);

public record CreateAddressRequest(
    string Label,
    AddressType Type,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsDefault = false);

public record UpdateAddressRequest(
    string Label,
    AddressType Type,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country,
    bool IsDefault = false);
