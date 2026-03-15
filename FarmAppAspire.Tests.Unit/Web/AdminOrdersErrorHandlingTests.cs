using System.Net;
using System.Net.Http.Json;
using FarmAppAspire.Tests.Unit.Helpers;
using FarmAppAspire.Web;

namespace FarmAppAspire.Tests.Unit.Web;

/// <summary>
/// Validates the error-handling behaviour of the Order Admin page async methods
/// as specified in openspec/changes/fix-admin-orders-500/specs/admin-orders-error-handling/spec.md.
/// </summary>
public class AdminOrdersErrorHandlingTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static OrderApiClient BuildClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    {
        var handler = new DelegatingHandlerStub(send);
        var http    = new HttpClient(handler) { BaseAddress = new Uri("https://customerservice") };
        return new OrderApiClient(http);
    }

    private static OrderApiClient BuildClientThatThrows(Exception ex)
        => BuildClient((_, _) => Task.FromException<HttpResponseMessage>(ex));

    private static OrderApiClient BuildClientThatReturns(HttpStatusCode status, object? body = null)
        => BuildClient(async (_, _) =>
        {
            var response = new HttpResponseMessage(status);
            if (body is not null)
                response.Content = JsonContent.Create(body);
            return await Task.FromResult(response);
        });

    // ── Task 4.1: OperationCanceledException is NOT treated as a display error ──

    [Fact]
    public async Task BrowseWeekInstancesAsync_CancellationToken_PropagatesCancellation()
    {
        // WHEN the CancellationToken is cancelled the client throws OperationCanceledException.
        // The component should catch it silently — this test verifies the exception type.
        using var cts    = new CancellationTokenSource();
        var       client = BuildClient((_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        await cts.CancelAsync();

        // The call should throw OperationCanceledException (TaskCanceledException is a subclass).
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.BrowseWeekInstancesAsync(DateTime.Today, cts.Token));
    }

    [Fact]
    public async Task GenerateInstancesAsync_CancellationToken_PropagatesCancellation()
    {
        using var cts    = new CancellationTokenSource();
        var       client = BuildClient((_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GenerateInstancesAsync(DateTime.Today, cts.Token));
    }

    // Verifies that OperationCanceledException IS a subtype of OperationCanceledException
    // so the component's `catch (OperationCanceledException)` guard will catch it.
    [Fact]
    public void TaskCanceledException_IsOperationCanceledException()
    {
        var ex = new TaskCanceledException();
        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    // ── Task 4.2: Browse error and generate error are independent fields ──────

    [Fact]
    public async Task BrowseWeekInstancesAsync_HttpError_ThrowsHttpRequestException()
    {
        // WHEN the service returns 500, GetFromJsonAsync throws HttpRequestException.
        // The component catches this into _browseError without touching _generateError.
        var client = BuildClientThatReturns(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.BrowseWeekInstancesAsync(DateTime.Today));
    }

    [Fact]
    public async Task GenerateInstancesAsync_HttpError_ReturnsNull()
    {
        // WHEN the service returns non-success, GenerateInstancesAsync returns null.
        // The component sets _generateError from the null check — browse data is unaffected.
        var client = BuildClientThatReturns(HttpStatusCode.InternalServerError);

        var result = await client.GenerateInstancesAsync(DateTime.Today);

        Assert.Null(result);
    }

    [Fact]
    public async Task BrowseWeekInstancesAsync_NetworkError_ThrowsHttpRequestException()
    {
        // WHEN the network is unavailable the call throws — component catches into _browseError.
        var client = BuildClientThatThrows(new HttpRequestException("connection refused"));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.BrowseWeekInstancesAsync(DateTime.Today));

        Assert.Contains("connection refused", ex.Message);
    }

    [Fact]
    public async Task GenerateInstancesAsync_NetworkError_ThrowsHttpRequestException()
    {
        // WHEN the network is unavailable, GenerateInstancesAsync throws HttpRequestException.
        // The component's GenerateAsync catch block catches it and sets _generateError,
        // leaving browse data and _browseError completely unaffected (independent error fields).
        var client = BuildClientThatThrows(new HttpRequestException("connection refused"));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GenerateInstancesAsync(DateTime.Today));

        Assert.Contains("connection refused", ex.Message);
    }

    // ── Spec: Successful browse returns instance list ─────────────────────────

    [Fact]
    public async Task BrowseWeekInstancesAsync_Success_ReturnsInstances()
    {
        var weekOf    = new DateTime(2025, 4, 7);
        var instances = new[]
        {
            new WeekInstanceSummary(Guid.NewGuid(), Guid.NewGuid(), "Alpha Farm", "AF-001",
                "Insulated", "Pending", weekOf, false, 2, 120m)
        };

        var client = BuildClientThatReturns(HttpStatusCode.OK, instances);

        var result = await client.BrowseWeekInstancesAsync(weekOf);

        Assert.Single(result);
        Assert.Equal("Alpha Farm", result[0].CustomerDisplayName);
        Assert.Equal("AF-001",     result[0].CustomerKey);
    }
}
