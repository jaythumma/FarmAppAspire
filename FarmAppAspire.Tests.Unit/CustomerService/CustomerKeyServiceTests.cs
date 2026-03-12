using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;

namespace FarmAppAspire.Tests.Unit.CustomerService;

public class CustomerKeyServiceTests
{
    private readonly CustomerKeyService _svc = new();

    // ── Abbreviation algorithm ────────────────────────────────────────────────

    [Theory]
    [InlineData("Sunrise",               "Dallas",   "TX", "SUN-DAL-TX")]
    [InlineData("Mangrove Foods",         "Chicago",  "IL", "MFA-CHI-IL")]
    [InlineData("Green Valley Foods LLC", "Austin",   "TX", "GVF-AUS-TX")]
    [InlineData("Blue Sky Inc",           "New York", "NY", "BSL-NEW-NY")]
    [InlineData("The Fresh Market",       "Atlanta",  "GA", "FMR-ATL-GA")]  // "The" ignored
    [InlineData("Co-Op Growers",          "Portland", "OR", "CGO-POR-OR")]
    public void Generate_ProducesExpectedKey(string displayName, string city, string state, string expected)
    {
        var customer = MakeCustomer(displayName, city, state);
        var key = _svc.Generate(customer);
        Assert.Equal(expected, key);
    }

    [Fact]
    public void Generate_CityLongerThan3Chars_TruncatesTo3()
    {
        var customer = MakeCustomer("Alpha", "Springfield", "IL");
        var key = _svc.Generate(customer);
        Assert.Equal("ALP-SPR-IL", key);
    }

    [Fact]
    public void Generate_CityExactly3Chars_UsesAll3()
    {
        var customer = MakeCustomer("Beta", "Elk", "WI");
        var key = _svc.Generate(customer);
        Assert.Equal("BET-ELK-WI", key);
    }

    // ── Collision detection ────────────────────────────────────────────────────

    [Fact]
    public void CheckCollision_ReturnsFalse_WhenKeyIsUnique()
    {
        var existing = new List<(string key, Guid id)> { ("SUN-DAL-TX", Guid.NewGuid()) };
        var thisId = Guid.NewGuid();
        Assert.False(CustomerKeyService.CheckCollision("MNF-CHI-IL", thisId, existing));
    }

    [Fact]
    public void CheckCollision_ReturnsTrue_WhenKeyExistsForDifferentCustomer()
    {
        var otherId = Guid.NewGuid();
        var existing = new List<(string key, Guid id)> { ("SUN-DAL-TX", otherId) };
        var thisId = Guid.NewGuid();
        Assert.True(CustomerKeyService.CheckCollision("SUN-DAL-TX", thisId, existing));
    }

    [Fact]
    public void CheckCollision_ReturnsFalse_WhenKeyBelongsToSameCustomer()
    {
        var thisId = Guid.NewGuid();
        var existing = new List<(string key, Guid id)> { ("SUN-DAL-TX", thisId) };
        Assert.False(CustomerKeyService.CheckCollision("SUN-DAL-TX", thisId, existing));
    }

    private static Customer MakeCustomer(string displayName, string city, string state) => new()
    {
        Id = Guid.NewGuid(),
        DisplayName = displayName,
        Addresses =
        [
            new CustomerAddress
            {
                Type = AddressType.Shipping, IsDefault = true,
                City = city, State = state,
                Line1 = "1 Main", PostalCode = "00000", Country = "US",
                CreatedAt = DateTime.UtcNow
            }
        ]
    };
}
