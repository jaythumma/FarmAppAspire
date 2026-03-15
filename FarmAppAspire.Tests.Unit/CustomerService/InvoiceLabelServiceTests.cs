using FarmAppAspire.CustomerService.Models;
using FarmAppAspire.CustomerService.Services;

namespace FarmAppAspire.Tests.Unit.CustomerService;

/// <summary>
/// Tests covering invoice label format requirements (Issue #47).
/// </summary>
public class InvoiceLabelServiceTests
{
    // ── Insulated channel format ──────────────────────────────────────────────

    [Theory]
    [InlineData("MAN-CHI-IL", 2024, 1,  "MAN-CHI-IL-2024-01")]
    [InlineData("MAN-CHI-IL", 2023, 7,  "MAN-CHI-IL-2023-07")]
    [InlineData("MAN-CHI-IL", 2024, 10, "MAN-CHI-IL-2024-10")]
    [InlineData("SUN-DAL-TX", 2025, 3,  "SUN-DAL-TX-2025-03")]
    public void BuildLabel_Insulated_ProducesCorrectFormat(
        string customerKey, int seasonYear, int seekNum, string expected)
    {
        var label = InvoiceLabelService.BuildLabel(customerKey, OrderChannel.Insulated, seasonYear, seekNum);
        Assert.Equal(expected, label);
    }

    // ── Scenario: First insulated invoice of a season is labeled correctly ───

    [Fact]
    public void BuildLabel_Insulated_FirstInvoice_SeekNum01()
    {
        var label = InvoiceLabelService.BuildLabel("MAN-CHI-IL", OrderChannel.Insulated, 2024, 1);
        Assert.Equal("MAN-CHI-IL-2024-01", label);
    }

    // ── Scenario: Seventh invoice for a sporadic customer ────────────────────

    [Fact]
    public void BuildLabel_Insulated_SeventhInvoice_SeekNum07()
    {
        var label = InvoiceLabelService.BuildLabel("SUN-DAL-TX", OrderChannel.Insulated, 2023, 7);
        Assert.Equal("SUN-DAL-TX-2023-07", label);
    }

    // ── FedEx channel format ──────────────────────────────────────────────────

    [Theory]
    [InlineData("MAN-CHI-IL", 2024, 3,  "AMZ-MAN-CHI-IL-2024-03")]
    [InlineData("SUN-DAL-TX", 2025, 1,  "AMZ-SUN-DAL-TX-2025-01")]
    [InlineData("GVF-AUS-TX", 2023, 12, "AMZ-GVF-AUS-TX-2023-12")]
    public void BuildLabel_FedEx_ProducesCorrectFormat(
        string customerKey, int seasonYear, int seekNum, string expected)
    {
        var label = InvoiceLabelService.BuildLabel(customerKey, OrderChannel.FedEx, seasonYear, seekNum);
        Assert.Equal(expected, label);
    }

    // ── Scenario: FedEx invoice label includes AMZ prefix ────────────────────

    [Fact]
    public void BuildLabel_FedEx_ThirdInvoice_HasAmzPrefixAndSeekNum03()
    {
        var label = InvoiceLabelService.BuildLabel("MAN-CHI-IL", OrderChannel.FedEx, 2024, 3);
        Assert.Equal("AMZ-MAN-CHI-IL-2024-03", label);
        Assert.StartsWith("AMZ-", label);
    }

    [Fact]
    public void BuildLabel_FedEx_HasAmzPrefix()
    {
        var label = InvoiceLabelService.BuildLabel("ABC-DEF-GH", OrderChannel.FedEx, 2024, 1);
        Assert.StartsWith("AMZ-", label);
    }

    [Fact]
    public void BuildLabel_Insulated_DoesNotHaveAmzPrefix()
    {
        var label = InvoiceLabelService.BuildLabel("ABC-DEF-GH", OrderChannel.Insulated, 2024, 1);
        Assert.DoesNotContain("AMZ-", label);
    }

    // ── SeekNum zero-padding ──────────────────────────────────────────────────

    [Theory]
    [InlineData(1,  "01")]
    [InlineData(7,  "07")]
    [InlineData(9,  "09")]
    [InlineData(10, "10")]
    [InlineData(99, "99")]
    public void BuildLabel_SeekNum_IsZeroPaddedToTwoDigits(int seekNum, string expectedPadded)
    {
        var label = InvoiceLabelService.BuildLabel("TST-LOC-XX", OrderChannel.Insulated, 2024, seekNum);
        Assert.EndsWith($"-{expectedPadded}", label);
    }
}
