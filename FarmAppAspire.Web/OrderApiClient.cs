namespace FarmAppAspire.Web;

public class OrderApiClient(HttpClient httpClient)
{
    public async Task<OrderSummary[]> GetOrdersAsync(
        Guid customerId, string? status = null, CancellationToken cancellationToken = default)
    {
        var url = $"/customers/{customerId}/order-instances"
                  + (status is not null ? $"?status={status}" : "");

        List<OrderSummary>? orders = null;
        await foreach (var order in httpClient.GetFromJsonAsAsyncEnumerable<OrderSummary>(url, cancellationToken))
        {
            if (order is not null)
            {
                orders ??= [];
                orders.Add(order);
            }
        }

        return orders?.ToArray() ?? [];
    }

    public async Task<OrderDetail?> GetOrderAsync(
        Guid customerId, Guid orderId, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<OrderDetail>(
            $"/customers/{customerId}/order-instances/{orderId}", cancellationToken);
}

public record OrderSummary(Guid Id, Guid CustomerId, string Status, DateTime WeekOf, bool IsSample);
public record OrderDetail(Guid Id, Guid CustomerId, string Status, DateTime WeekOf, bool IsSample);
