using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace FarmAppAspire.Web;

public class CustomerApiClientHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?
            .User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is not null)
            request.Headers.TryAddWithoutValidation("X-User-Id", userId);

        return base.SendAsync(request, cancellationToken);
    }
}
