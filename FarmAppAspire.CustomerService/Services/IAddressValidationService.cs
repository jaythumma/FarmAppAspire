namespace FarmAppAspire.CustomerService.Services;

/// <summary>
/// Result of an address validation attempt.
/// </summary>
/// <param name="IsValid">Whether the address passed validation.</param>
/// <param name="Errors">Validation error messages; empty when <see cref="IsValid"/> is <c>true</c>.</param>
public record AddressValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    private static readonly AddressValidationResult ValidResult =
        new(true, Array.Empty<string>());

    public static AddressValidationResult Valid() => ValidResult;

    public static AddressValidationResult Invalid(params string[] errors) =>
        new(false, errors);
}

/// <summary>
/// Validates a postal address.
/// Implementations can perform local format checks or delegate to an external
/// verification provider such as USPS, Smarty, or Google Maps.
/// </summary>
public interface IAddressValidationService
{
    /// <summary>
    /// Validates the given address fields.
    /// </summary>
    Task<AddressValidationResult> ValidateAsync(
        string line1,
        string? line2,
        string city,
        string state,
        string postalCode,
        string country,
        CancellationToken cancellationToken = default);
}
