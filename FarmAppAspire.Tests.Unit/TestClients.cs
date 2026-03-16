using System.Net.Http;

namespace FarmAppAspire.Tests.Unit;

public static class TestClients
{
    public static HttpClient ApiClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? handler = null)
    {
        if (handler is null)
            return new HttpClient() { BaseAddress = new Uri("https://customerservice") };
        var stub = new FarmAppAspire.Tests.Unit.Helpers.DelegatingHandlerStub(handler);
        return new HttpClient(stub) { BaseAddress = new Uri("https://customerservice") };
    }
}
