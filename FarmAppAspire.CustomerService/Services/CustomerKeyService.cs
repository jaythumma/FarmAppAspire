using System.Text.RegularExpressions;
using FarmAppAspire.CustomerService.Models;

namespace FarmAppAspire.CustomerService.Services;

/// <summary>
/// Generates and validates CustomerKey identifiers.
/// Standard key format:  {NameAbbr}-{CityAbbr}-{StateAbbr}
/// Amazon channel format: AMZ-{NameAbbr}-{CityAbbr}-{StateAbbr}
/// e.g. "Green Valley Foods LLC" + Chicago, IL → "GVF-CHI-IL" (Direct)
///                                              → "AMZ-GVF-CHI-IL" (Amazon)
/// </summary>
public class CustomerKeyService
{
    private static readonly HashSet<string> IgnoredWords =
        new(StringComparer.OrdinalIgnoreCase) { "llc", "inc", "co", "the", "corp", "ltd" };

    /// <summary>Generates a CustomerKey from the customer's display name and primary shipping address.</summary>
    public string Generate(Customer customer)
    {
        var primaryAddress = customer.Addresses
            .FirstOrDefault(a => a.Type == AddressType.Shipping && a.IsDefault)
            ?? customer.Addresses.FirstOrDefault(a => a.Type == AddressType.Shipping)
            ?? customer.Addresses.FirstOrDefault();

        var city  = primaryAddress?.City  ?? string.Empty;
        var state = primaryAddress?.State ?? string.Empty;

        var nameAbbr = BuildNameAbbr(customer.DisplayName);
        var cityAbbr = BuildCityAbbr(city);

        if (customer.ChannelType == ChannelType.Amazon)
            return $"AMZ-{nameAbbr}-{cityAbbr}-{state.ToUpperInvariant()}";

        return $"{nameAbbr}-{cityAbbr}-{state.ToUpperInvariant()}";
    }

    /// <summary>
    /// Returns true if the proposed key is already used by a different customer.
    /// </summary>
    public static bool CheckCollision(string proposedKey,
        Guid customerId,
        IEnumerable<(string key, Guid id)> existingKeys) =>
        existingKeys.Any(e =>
            string.Equals(e.key, proposedKey, StringComparison.OrdinalIgnoreCase)
            && e.id != customerId);

    // ── Abbreviation helpers ──────────────────────────────────────────────────

    private static string BuildNameAbbr(string displayName)
    {
        var words = displayName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !IgnoredWords.Contains(w.Trim('.'  )))
            .ToList();

        if (words.Count == 0) return "UNK";

        // First letter of each significant word (up to 3)
        var abbr = string.Concat(words.Take(3).Select(w => char.ToUpperInvariant(w[0])));

        // Pad to 3 chars using subsequent chars from the first significant word
        var i = 1;
        while (abbr.Length < 3 && i < words[0].Length)
        {
            abbr += char.ToUpperInvariant(words[0][i]);
            i++;
        }

        return Truncate(abbr, 3);
    }

    private static string BuildCityAbbr(string city) =>
        Truncate(Regex.Replace(city.Trim(), @"\s+", ""), 3).ToUpperInvariant();

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max];
}
