using FarmAppAspire.Web;

namespace FarmAppAspire.Tests.Unit.Web;

/// <summary>
/// Validates <see cref="OrderContactRules"/> contact assignment rules (Issue #39).
/// </summary>
public class OrderContactAssignmentTests
{
    // ── ContactIsRequired ──────────────────────────────────────────────────────

    [Fact]
    public void ContactIsRequired_WholesaleCustomer_ReturnsTrue()
    {
        Assert.True(OrderContactRules.ContactIsRequired(CustomerType.Wholesale));
    }

    [Fact]
    public void ContactIsRequired_RetailCustomer_ReturnsFalse()
    {
        Assert.False(OrderContactRules.ContactIsRequired(CustomerType.Retail));
    }

    // ── ValidateContact – Wholesale ────────────────────────────────────────────

    [Fact]
    public void ValidateContact_WholesaleWithNoContact_ReturnsErrorMessage()
    {
        // Scenario: Wholesale customer selected — ContactId field is required
        // WHEN staff selects a wholesale customer and attempts to submit without selecting a contact
        // THEN the form displays "A contact is required for wholesale customers."
        var error = OrderContactRules.ValidateContact(CustomerType.Wholesale, null);

        Assert.Equal("A contact is required for wholesale customers.", error);
    }

    [Fact]
    public void ValidateContact_WholesaleWithEmptyStringContact_ReturnsErrorMessage()
    {
        var error = OrderContactRules.ValidateContact(CustomerType.Wholesale, "");

        Assert.Equal("A contact is required for wholesale customers.", error);
    }

    [Fact]
    public void ValidateContact_WholesaleWithValidContactId_ReturnsNull()
    {
        // WHEN a wholesale customer has a valid contact selected, validation passes
        var error = OrderContactRules.ValidateContact(CustomerType.Wholesale, Guid.NewGuid().ToString());

        Assert.Null(error);
    }

    // ── ValidateContact – Retail ───────────────────────────────────────────────

    [Fact]
    public void ValidateContact_RetailWithNoContact_ReturnsNull()
    {
        // Scenario: Retail customer selected — ContactId field is optional
        // WHEN staff selects a retail customer and submits without selecting a contact
        // THEN the form submits successfully with ContactId = null
        var error = OrderContactRules.ValidateContact(CustomerType.Retail, null);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateContact_RetailWithEmptyStringContact_ReturnsNull()
    {
        var error = OrderContactRules.ValidateContact(CustomerType.Retail, "");

        Assert.Null(error);
    }

    [Fact]
    public void ValidateContact_RetailWithValidContactId_ReturnsNull()
    {
        var error = OrderContactRules.ValidateContact(CustomerType.Retail, Guid.NewGuid().ToString());

        Assert.Null(error);
    }

    // ── Switching customer clears contact ──────────────────────────────────────

    [Fact]
    public void ValidateContact_AfterSwitchingFromWholesaleToRetail_NoContactRequired()
    {
        // Scenario: Switching customer clears the previously selected contact
        // WHEN staff changes from a wholesale customer (had contact) to a retail customer
        // THEN no contact is required (the previously selected contact would be cleared by the form)
        var error = OrderContactRules.ValidateContact(CustomerType.Retail, null);

        Assert.Null(error);
    }

    [Fact]
    public void ValidateContact_AfterSwitchingFromRetailToWholesale_ContactRequiredWithoutContact()
    {
        // WHEN staff changes from retail to wholesale and the cleared contact is still empty
        // THEN validation requires a contact to be selected
        var error = OrderContactRules.ValidateContact(CustomerType.Wholesale, null);

        Assert.Equal("A contact is required for wholesale customers.", error);
    }
}
