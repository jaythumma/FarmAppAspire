using FarmAppAspire.CustomerService.Services;

namespace FarmAppAspire.Tests.Unit.CustomerService;

/// <summary>
/// Unit tests for <see cref="FormatAddressValidationService"/>.
/// </summary>
public class AddressValidationServiceTests
{
    private readonly FormatAddressValidationService _sut = new();

    // ── Valid addresses ───────────────────────────────────────────────────────

    [Fact]
    public async Task ValidUSAddress_ReturnsValid()
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Springfield", "IL", "62701", "US");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidUSAddress_WithZipPlusFour_ReturnsValid()
    {
        var result = await _sut.ValidateAsync("100 Oak Ave", "Suite 5", "Chicago", "IL", "60601-1234", "US");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidCanadianAddress_ReturnsValid()
    {
        var result = await _sut.ValidateAsync("123 Maple St", null, "Toronto", "ON", "M5V 3A8", "CA");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidCanadianAddress_WithHyphenSeparator_ReturnsValid()
    {
        var result = await _sut.ValidateAsync("55 King St", null, "Vancouver", "BC", "V6B-1A1", "CA");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // ── Required-field validation ─────────────────────────────────────────────

    [Fact]
    public async Task MissingLine1_ReturnsInvalid()
    {
        var result = await _sut.ValidateAsync("", null, "Springfield", "IL", "62701", "US");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Line 1"));
    }

    [Fact]
    public async Task WhiteSpaceLine1_ReturnsInvalid()
    {
        var result = await _sut.ValidateAsync("   ", null, "Springfield", "IL", "62701", "US");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Line 1"));
    }

    [Fact]
    public async Task MissingCity_ReturnsInvalid()
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "", "IL", "62701", "US");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("City"));
    }

    [Fact]
    public async Task MissingState_ReturnsInvalid()
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Springfield", "", "62701", "US");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("State"));
    }

    [Fact]
    public async Task MissingPostalCode_ReturnsInvalid()
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Springfield", "IL", "", "US");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Postal code"));
    }

    [Fact]
    public async Task MissingCountry_ReturnsInvalid()
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Springfield", "IL", "62701", "");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Country"));
    }

    // ── Country validation ────────────────────────────────────────────────────

    [Fact]
    public async Task UnsupportedCountry_ReturnsInvalid()
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "London", "ENG", "SW1A 1AA", "GB");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("GB") && e.Contains("not supported"));
    }

    [Theory]
    [InlineData("US")]
    [InlineData("us")]
    [InlineData("CA")]
    [InlineData("ca")]
    public async Task SupportedCountries_AreAccepted(string country)
    {
        var state = country.Equals("CA", StringComparison.OrdinalIgnoreCase) ? "ON" : "IL";
        var zip   = country.Equals("CA", StringComparison.OrdinalIgnoreCase) ? "M5V 3A8" : "62701";

        var result = await _sut.ValidateAsync("1 Main St", null, "City", state, zip, country);

        Assert.True(result.IsValid, $"Expected valid for country '{country}', but got errors: {string.Join("; ", result.Errors)}");
    }

    // ── US state validation ───────────────────────────────────────────────────

    [Theory]
    [InlineData("IL")]
    [InlineData("CA")]
    [InlineData("TX")]
    [InlineData("DC")]
    [InlineData("PR")]
    public async Task ValidUSState_ReturnsValid(string state)
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Anytown", state, "62701", "US");

        Assert.True(result.IsValid, $"Expected valid for state '{state}', but got errors: {string.Join("; ", result.Errors)}");
    }

    [Fact]
    public async Task InvalidUSState_ReturnsInvalid()
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Springfield", "XX", "62701", "US");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("XX") && e.Contains("US state"));
    }

    [Fact]
    public async Task CanadianProvinceForUSAddress_ReturnsInvalid()
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Toronto", "ON", "62701", "US");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("ON") && e.Contains("US state"));
    }

    // ── Canadian province validation ──────────────────────────────────────────

    [Theory]
    [InlineData("ON")]
    [InlineData("BC")]
    [InlineData("AB")]
    [InlineData("QC")]
    public async Task ValidCanadianProvince_ReturnsValid(string province)
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Anytown", province, "M5V 3A8", "CA");

        Assert.True(result.IsValid, $"Expected valid for province '{province}', but got errors: {string.Join("; ", result.Errors)}");
    }

    [Fact]
    public async Task InvalidCanadianProvince_ReturnsInvalid()
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Toronto", "XX", "M5V 3A8", "CA");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("XX") && e.Contains("Canadian province"));
    }

    // ── US ZIP code validation ────────────────────────────────────────────────

    [Theory]
    [InlineData("62701")]
    [InlineData("00501")]
    [InlineData("62701-1234")]
    [InlineData("90210-5678")]
    public async Task ValidUSZip_ReturnsValid(string zip)
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Springfield", "IL", zip, "US");

        Assert.True(result.IsValid, $"Expected valid for ZIP '{zip}', but got errors: {string.Join("; ", result.Errors)}");
    }

    [Theory]
    [InlineData("6270")]        // too short
    [InlineData("627011")]      // too long
    [InlineData("ABCDE")]       // letters
    [InlineData("62701-123")]   // zip+4 too short
    [InlineData("62701-12345")] // zip+4 too long
    public async Task InvalidUSZip_ReturnsInvalid(string zip)
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Springfield", "IL", zip, "US");

        Assert.False(result.IsValid, $"Expected invalid for ZIP '{zip}'");
        Assert.Contains(result.Errors, e => e.Contains(zip) && e.Contains("ZIP code"));
    }

    // ── Canadian postal code validation ──────────────────────────────────────

    [Theory]
    [InlineData("M5V 3A8")]
    [InlineData("V6B1A1")]
    [InlineData("K1A-0A6")]
    public async Task ValidCanadianPostalCode_ReturnsValid(string postalCode)
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Toronto", "ON", postalCode, "CA");

        Assert.True(result.IsValid, $"Expected valid for postal code '{postalCode}', but got errors: {string.Join("; ", result.Errors)}");
    }

    [Theory]
    [InlineData("12345")]    // US ZIP
    [InlineData("ABCDEF")]   // wrong pattern
    [InlineData("M5V3A")]    // too short
    public async Task InvalidCanadianPostalCode_ReturnsInvalid(string postalCode)
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Toronto", "ON", postalCode, "CA");

        Assert.False(result.IsValid, $"Expected invalid for postal code '{postalCode}'");
        Assert.Contains(result.Errors, e => e.Contains("postal code"));
    }

    // ── Multiple errors accumulate ────────────────────────────────────────────

    [Fact]
    public async Task MultipleInvalidFields_ReturnsAllErrors()
    {
        var result = await _sut.ValidateAsync("", null, "", "", "", "US");

        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 3, $"Expected at least 3 errors, got {result.Errors.Count}: {string.Join("; ", result.Errors)}");
    }

    // ── AddressValidationResult factory methods ───────────────────────────────

    [Fact]
    public void Valid_ReturnsIsValidTrue_WithNoErrors()
    {
        var result = AddressValidationResult.Valid();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Invalid_ReturnsIsValidFalse_WithErrors()
    {
        var result = AddressValidationResult.Invalid("Error A", "Error B");

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.Contains("Error A", result.Errors);
        Assert.Contains("Error B", result.Errors);
    }

    // ── Line2 is optional ─────────────────────────────────────────────────────

    [Fact]
    public async Task NullLine2_DoesNotCauseValidationError()
    {
        var result = await _sut.ValidateAsync("1 Main St", null, "Springfield", "IL", "62701", "US");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task NonNullLine2_IsIgnoredInValidation()
    {
        var result = await _sut.ValidateAsync("1 Main St", "Apt 4B", "Springfield", "IL", "62701", "US");

        Assert.True(result.IsValid);
    }

    // ── Cancellation token is accepted ───────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_AcceptsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var result = await _sut.ValidateAsync("1 Main St", null, "Springfield", "IL", "62701", "US", cts.Token);

        Assert.True(result.IsValid);
    }
}
