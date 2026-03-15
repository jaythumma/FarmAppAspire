using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FarmAppAspire.Tests.Unit.Helpers;
using FarmAppAspire.Web;

namespace FarmAppAspire.Tests.Unit.Web;

public class CustomerApiClientContactTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static CustomerApiClient BuildClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        var stub = new DelegatingHandlerStub(handler);
        var http = new HttpClient(stub) { BaseAddress = new Uri("https://customerservice") };
        return new CustomerApiClient(http);
    }

    [Fact]
    public async Task CreateContactAsync_Returns_ContactDto_On_201()
    {
        var customerId = Guid.NewGuid();
        var expected = new ContactDto(Guid.NewGuid(), ContactRole.Primary, "Jane", "Doe", "jane@example.com", null, null, true);

        var client = BuildClient((_, ct) =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.Created);
            resp.Content = JsonContent.Create(expected, options: JsonOptions);
            return Task.FromResult(resp);
        });

        var request = new CreateContactRequest(ContactRole.Primary, "Jane", "Doe", "jane@example.com", null, null, true);
        var result = await client.CreateContactAsync(customerId, request, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(expected.Id, result.Id);
        Assert.Equal(expected.FirstName, result.FirstName);
        Assert.Equal(expected.Role, result.Role);
    }

    [Fact]
    public async Task UpdateContactAsync_Returns_ContactDto_On_200()
    {
        var customerId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var expected = new ContactDto(contactId, ContactRole.Billing, "John", "Smith", null, "555-1234", null, false);

        var client = BuildClient((_, ct) =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.OK);
            resp.Content = JsonContent.Create(expected, options: JsonOptions);
            return Task.FromResult(resp);
        });

        var request = new UpdateContactRequest(ContactRole.Billing, "John", "Smith", null, "555-1234", null, false);
        var result = await client.UpdateContactAsync(customerId, contactId, request, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(contactId, result.Id);
        Assert.Equal(ContactRole.Billing, result.Role);
        Assert.Equal("555-1234", result.Phone);
    }

    [Fact]
    public async Task DeleteContactAsync_Returns_True_On_204()
    {
        var customerId = Guid.NewGuid();
        var contactId = Guid.NewGuid();

        var client = BuildClient((_, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        var result = await client.DeleteContactAsync(customerId, contactId, TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteContactAsync_Returns_False_On_404()
    {
        var customerId = Guid.NewGuid();
        var contactId = Guid.NewGuid();

        var client = BuildClient((_, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await client.DeleteContactAsync(customerId, contactId, TestContext.Current.CancellationToken);

        Assert.False(result);
    }
}
