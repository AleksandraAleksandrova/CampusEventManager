using System.Net.Http.Headers;
using System.Text.Json;

namespace CampusEventManager.Web.Services;

public class AuthService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly TokenStorage _storage;
    public CampusEventManager.Web.Models.UserProfile? CurrentUser { get; private set; }
    public string? Token { get; private set; }
    public bool IsRestored { get; private set; }

    public AuthService(IHttpClientFactory httpFactory, TokenStorage storage)
    {
        _httpFactory = httpFactory;
        _storage = storage;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var client = _httpFactory.CreateClient("registration");
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        if (!resp.IsSuccessStatusCode) return false;
        var obj = await resp.Content.ReadFromJsonAsync<JsonDocument>();
        if (obj is not null && obj.RootElement.ValueKind == JsonValueKind.Object && obj.RootElement.TryGetProperty("access_token", out var t))
        {
            Token = t.GetString();
            if (!string.IsNullOrEmpty(Token))
            {
                await _storage.SetTokenAsync(Token);
            }
            // populate current user
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var meResp = await client.GetAsync("/api/auth/me");
            if (!meResp.IsSuccessStatusCode) return false;
            CurrentUser = await meResp.Content.ReadFromJsonAsync<CampusEventManager.Web.Models.UserProfile>();
            return CurrentUser is not null;
        }

        return false;
    }

    public void Logout()
    {
        Token = null;
        CurrentUser = null;
        _ = _storage.RemoveTokenAsync();
    }

    // Returns: true = restored and authenticated, false = restored and not authenticated,
    // null = prerendering blocked (JS not available) - caller should retry later
    public async Task<bool?> TryRestoreAsync()
    {
        try
        {
            var tok = await _storage.GetTokenAsync();
            if (string.IsNullOrEmpty(tok))
            {
                IsRestored = true;
                return false;
            }

            Token = tok;
            var client = _httpFactory.CreateClient("registration");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);
            var meResp = await client.GetAsync("/api/auth/me");
            if (!meResp.IsSuccessStatusCode)
            {
                // If unauthorized, clear stored token; otherwise treat as transient failure and keep token.
                if (meResp.StatusCode == System.Net.HttpStatusCode.Unauthorized || meResp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Token = null;
                    await _storage.RemoveTokenAsync();
                    IsRestored = true;
                    return false;
                }

                // Transient or server error: mark restored so UI can decide; do not remove token.
                IsRestored = true;
                return false;
            }

            CurrentUser = await meResp.Content.ReadFromJsonAsync<CampusEventManager.Web.Models.UserProfile>();
            IsRestored = true;
            return CurrentUser is not null;
        }
        catch (InvalidOperationException)
        {
            // JS interop not available in prerender; signal caller to retry later
            return null;
        }
        catch
        {
            // Transient errors: mark restored to avoid indefinite wait, but treat as not authenticated
            IsRestored = true;
            return false;
        }
    }
}
