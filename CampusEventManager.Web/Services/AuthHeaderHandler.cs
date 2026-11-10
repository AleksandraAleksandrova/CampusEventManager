using System.Net.Http.Headers;

namespace CampusEventManager.Web.Services;

// Resolves AuthService per-send from the scoped container so the handler
// always sees the token that belongs to the current Blazor circuit scope,
// regardless of when the HttpClient pipeline was constructed.
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IServiceProvider _sp;

    public AuthHeaderHandler(IServiceProvider sp)
    {
        _sp = sp;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var auth = _sp.GetService<AuthService>();
        if (auth is not null && !string.IsNullOrEmpty(auth.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
