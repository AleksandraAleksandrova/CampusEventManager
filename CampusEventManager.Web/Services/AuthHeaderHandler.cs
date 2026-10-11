using System.Net.Http.Headers;

namespace CampusEventManager.Web.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly AuthService _auth;

    public AuthHeaderHandler(AuthService auth)
    {
        _auth = auth;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_auth.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
