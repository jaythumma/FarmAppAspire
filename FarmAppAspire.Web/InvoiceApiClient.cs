namespace FarmAppAspire.Web;

public class InvoiceApiClient(HttpClient httpClient)
{
    public async Task<InvoiceSummary[]> GetInvoicesAsync(
        Guid customerId, int? seasonYear = null, string? channel = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (seasonYear.HasValue) query.Add($"seasonYear={seasonYear}");
        if (!string.IsNullOrEmpty(channel)) query.Add($"channel={channel}");
        var url = $"/customers/{customerId}/invoices"
                  + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        List<InvoiceSummary>? invoices = null;
        await foreach (var invoice in httpClient.GetFromJsonAsAsyncEnumerable<InvoiceSummary>(url, cancellationToken))
        {
            if (invoice is not null)
            {
                invoices ??= [];
                invoices.Add(invoice);
            }
        }

        return invoices?.ToArray() ?? [];
    }

    public async Task<InvoiceSummary?> GetInvoiceAsync(
        Guid customerId, Guid invoiceId, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<InvoiceSummary>(
            $"/customers/{customerId}/invoices/{invoiceId}", cancellationToken);
}

public record InvoiceSummary(Guid Id, Guid OrderInstanceId, Guid CustomerId,
    string Channel, int SeasonYear, int SeekNum, string Label, DateTime CreatedAt);
