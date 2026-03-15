using System.Text.RegularExpressions;

namespace FarmAppAspire.CustomerService.Services;

/// <summary>
/// Format-based address validator that checks US and Canadian postal addresses
/// without calling any external service. All network-based verification is left
/// to the <see cref="IAddressValidationService"/> contract so that a real
/// provider (USPS, Smarty, Google Maps, etc.) can be swapped in by registering
/// a different implementation in the DI container.
/// </summary>
public partial class FormatAddressValidationService : IAddressValidationService
{
    // ── US state / territory abbreviations ───────────────────────────────────
    private static readonly HashSet<string> UsStateAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA",
        "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD",
        "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ",
        "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC",
        "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY",
        // US territories
        "DC", "AS", "GU", "MP", "PR", "VI"
    };

    // ── Canadian province / territory abbreviations ───────────────────────────
    private static readonly HashSet<string> CaProvinceAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "AB", "BC", "MB", "NB", "NL", "NS", "NT", "NU", "ON", "PE", "QC", "SK", "YT"
    };

    // ── Supported country codes ───────────────────────────────────────────────
    private static readonly HashSet<string> SupportedCountries = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "CA"
    };

    // ── Postal-code patterns ──────────────────────────────────────────────────
    [GeneratedRegex(@"^\d{5}(-\d{4})?$")]
    private static partial Regex UsZipPattern();

    [GeneratedRegex(@"^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$")]
    private static partial Regex CaPostalPattern();

    /// <inheritdoc />
    public Task<AddressValidationResult> ValidateAsync(
        string line1,
        string? line2,
        string city,
        string state,
        string postalCode,
        string country,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(line1))
            errors.Add("Street address (Line 1) is required.");

        if (string.IsNullOrWhiteSpace(city))
            errors.Add("City is required.");

        if (string.IsNullOrWhiteSpace(state))
        {
            errors.Add("State / province is required.");
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            errors.Add("Postal code is required.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            errors.Add("Country is required.");
        }
        else if (!SupportedCountries.Contains(country))
        {
            errors.Add($"Country '{country}' is not supported. Accepted values: US, CA.");
        }
        else
        {
            // Country-specific state and postal-code validation
            var normalizedCountry = country.Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(state))
            {
                if (normalizedCountry == "US" && !UsStateAbbreviations.Contains(state.Trim()))
                    errors.Add($"'{state}' is not a valid US state or territory abbreviation.");
                else if (normalizedCountry == "CA" && !CaProvinceAbbreviations.Contains(state.Trim()))
                    errors.Add($"'{state}' is not a valid Canadian province or territory abbreviation.");
            }

            if (!string.IsNullOrWhiteSpace(postalCode))
            {
                if (normalizedCountry == "US" && !UsZipPattern().IsMatch(postalCode.Trim()))
                    errors.Add($"'{postalCode}' is not a valid US ZIP code (expected 5 digits or 5+4 format, e.g. 62701 or 62701-1234).");
                else if (normalizedCountry == "CA" && !CaPostalPattern().IsMatch(postalCode.Trim()))
                    errors.Add($"'{postalCode}' is not a valid Canadian postal code (expected format A1A 1A1).");
            }
        }

        var result = errors.Count == 0
            ? AddressValidationResult.Valid()
            : AddressValidationResult.Invalid([.. errors]);

        return Task.FromResult(result);
    }
}
