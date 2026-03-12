using FarmAppAspire.CustomerService.Models;

namespace FarmAppAspire.CustomerService.Services;

public static class InvoiceLabelService
{
    /// <summary>
    /// Builds the invoice label.
    /// Insulated: {CustomerKey}-{SeasonYear}-{SeekNum:D2}
    /// FedEx:    AMZ-{CustomerKey}-{SeasonYear}-{SeekNum:D2}
    /// </summary>
    public static string BuildLabel(string customerKey, OrderChannel channel, int seasonYear, int seekNum)
    {
        var core = $"{customerKey}-{seasonYear}-{seekNum:D2}";
        return channel == OrderChannel.FedEx ? $"AMZ-{core}" : core;
    }
}
