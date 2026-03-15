namespace FarmAppAspire.Web;

/// <summary>
/// Business rules that govern when a contact is required on a new order.
/// </summary>
public static class OrderContactRules
{
    /// <summary>
    /// Returns <c>true</c> when the given customer type mandates a contact selection.
    /// Wholesale customers always require a contact; retail customers do not.
    /// </summary>
    public static bool ContactIsRequired(CustomerType customerType) =>
        customerType == CustomerType.Wholesale;

    /// <summary>
    /// Validates the contact selection against the customer type.
    /// Returns an error message when validation fails, or <c>null</c> when the selection is valid.
    /// </summary>
    public static string? ValidateContact(CustomerType customerType, string? contactId) =>
        ContactIsRequired(customerType) && string.IsNullOrEmpty(contactId)
            ? "A contact is required for wholesale customers."
            : null;
}
