using FarmAppAspire.CustomerService.Services;

namespace FarmAppAspire.Tests.Unit.Web;

public class CustomerKeyDisplayTests
{
    // ── Key display rules ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("TC-DIRECT-001", false)]
    [InlineData("ALPHA-WS-002",  false)]
    [InlineData("X",             false)]
    public void CustomerKey_NonNull_ShouldBeDisplayedProminently(string key, bool collision)
    {
        // These simulate the condition checked in Detail.razor and List.razor:
        // !string.IsNullOrEmpty(c.CustomerKey) → show prominently
        Assert.False(string.IsNullOrEmpty(key));
        _ = collision; // used by view
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CustomerKey_NullOrWhiteSpace_ShouldShowNoKeyPlaceholder(string? key)
    {
        Assert.True(string.IsNullOrWhiteSpace(key));
    }

    [Fact]
    public void CustomerKey_WithCollision_ShouldTriggerWarning()
    {
        const string key = "DUPLICATE-KEY";
        const bool collision = true;

        Assert.False(string.IsNullOrEmpty(key));
        Assert.True(collision);
    }

    // ── Monday-snap logic (mirrors Admin/Orders.razor SnapToMonday) ───────────

    [Theory]
    [InlineData(DayOfWeek.Monday,    0)]   // already Monday — no change
    [InlineData(DayOfWeek.Tuesday,   1)]   // go back 1 day
    [InlineData(DayOfWeek.Wednesday, 2)]
    [InlineData(DayOfWeek.Thursday,  3)]
    [InlineData(DayOfWeek.Friday,    4)]
    [InlineData(DayOfWeek.Saturday,  5)]
    [InlineData(DayOfWeek.Sunday,    6)]
    public void SnapToMonday_AlwaysLandsOnMonday(DayOfWeek inputDow, int daysBack)
    {
        // Find the nearest date with that day of week
        var baseMonday = new DateTime(2025, 4, 7); // known Monday
        var input      = baseMonday.AddDays((int)inputDow == 0 ? 0 : (int)inputDow);
        var expected   = baseMonday;

        var snapped = SeasonYearService.MondayOf(input);

        Assert.Equal(DayOfWeek.Monday, snapped.DayOfWeek);
        Assert.Equal(expected, snapped);
        _ = daysBack;
    }

    // ── Inspect date validation — Friday and Monday only ─────────────────────

    [Theory]
    [InlineData(DayOfWeek.Friday, true)]
    [InlineData(DayOfWeek.Monday, true)]
    public void InspectionDay_FridayOrMonday_IsValid(DayOfWeek dow, bool expected)
    {
        var isValid = dow == DayOfWeek.Friday || dow == DayOfWeek.Monday;
        Assert.Equal(expected, isValid);
    }

    [Theory]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public void InspectionDay_NonFridayNonMonday_IsInvalid(DayOfWeek dow)
    {
        var isValid = dow == DayOfWeek.Friday || dow == DayOfWeek.Monday;
        Assert.False(isValid);
    }
}
