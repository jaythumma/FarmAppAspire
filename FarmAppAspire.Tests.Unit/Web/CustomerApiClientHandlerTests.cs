using System.Security.Claims;
using FarmAppAspire.Tests.Unit.Helpers;
using FarmAppAspire.Web;
using Microsoft.AspNetCore.Http;

namespace FarmAppAspire.Tests.Unit.Web;

public class CustomerApiClientHandlerTests
{
    private static (CustomerApiClientHandler handler, Func<HttpRequestMessage?> getCapture) BuildHandler(
        ClaimsPrincipal user)
    {
        var accessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = user };
        accessor.Setup(x => x.HttpContext).Returns(httpContext);

        HttpRequestMessage? captured = null;
        var stub = new DelegatingHandlerStub((req, _) =>
        {
            captured = req;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        });

        var handler = new CustomerApiClientHandler(accessor.Object) { InnerHandler = stub };
        return (handler, () => captured);
    }

    [Fact]
    public async Task SetsXUserIdHeader_FromNameIdentifierClaim()
    {
        var userId = "test-user-guid-123";
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)]));

        var (handler, getCapture) = BuildHandler(user);
        var client = new HttpClient(handler);

        await client.GetAsync("https://customerservice/api", TestContext.Current.CancellationToken);

        var request = getCapture();
        Assert.NotNull(request);
        Assert.True(request.Headers.Contains("X-User-Id"));
        Assert.Equal(userId, request.Headers.GetValues("X-User-Id").Single());
    }

    [Fact]
    public async Task NoXUserIdHeader_WhenClaimAbsent()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var (handler, getCapture) = BuildHandler(user);
        var client = new HttpClient(handler);

        await client.GetAsync("https://customerservice/api", TestContext.Current.CancellationToken);

        var request = getCapture();
        Assert.NotNull(request);
        Assert.False(request.Headers.Contains("X-User-Id"));
    }

    [Fact]
    public async Task NoXUserIdHeader_WhenHttpContextIsNull()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        HttpRequestMessage? captured = null;
        var stub = new DelegatingHandlerStub((req, _) =>
        {
            captured = req;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        });

        var handler = new CustomerApiClientHandler(accessor.Object) { InnerHandler = stub };
        var client = new HttpClient(handler);

        await client.GetAsync("https://customerservice/api", TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.False(captured.Headers.Contains("X-User-Id"));
    }
}
