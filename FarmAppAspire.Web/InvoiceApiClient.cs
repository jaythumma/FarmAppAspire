namespace FarmAppAspire.Web;

public class InvoiceApiClient(HttpClient httpClient)
{
    public async Task<InvoiceSummary[]> GetInvoicesAsync(
        Guid customerId, int? seasonYear = null, CancellationToken cancellationToken = default)
    {
        var url = $"/customers/{customerId}/invoices"
                  + (seasonYear.HasValue ? $"?seasonYear={seasonYear}" : "");

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
}

public record InvoiceSummary(Guid Id, Guid OrderInstanceId, Guid CustomerId,
    string Channel, int SeasonYear, int SeekNum, string Label, DateTime CreatedAt);
