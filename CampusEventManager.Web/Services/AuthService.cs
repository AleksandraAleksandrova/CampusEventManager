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
    public event Action? OnChange;
    public event Action<int>? OnUnreadCountChanged;
    public event Action? OnUnreadRefreshRequested;

    public AuthService(IHttpClientFactory httpFactory, TokenStorage storage)
    {
        _httpFactory = httpFactory;
        _storage = storage;
    }

    // Returns an HttpClient with the current Bearer token already set.
    public HttpClient CreateAuthorizedClient(string name)
    {
        var client = _httpFactory.CreateClient(name);
        if (!string.IsNullOrEmpty(Token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        return client;
    }

    public void NotifyUnreadCount(int count) => OnUnreadCountChanged?.Invoke(count);
    public void RequestUnreadRefresh() => OnUnreadRefreshRequested?.Invoke();

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
                await _storage.SetTokenAsync(Token);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var meResp = await client.GetAsync("/api/auth/me");
            if (!meResp.IsSuccessStatusCode) return false;
            CurrentUser = await meResp.Content.ReadFromJsonAsync<CampusEventManager.Web.Models.UserProfile>();
            if (CurrentUser is not null)
            {
                IsRestored = true;
                OnChange?.Invoke();
                return true;
            }
            return false;
        }
        return false;
    }

    public void Logout()
    {
        Token = null;
        CurrentUser = null;
        IsRestored = true;
        _ = _storage.RemoveTokenAsync();
        OnChange?.Invoke();
        OnUnreadCountChanged?.Invoke(0);
    }

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
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            var meResp = await client.GetAsync("/api/auth/me");
            if (!meResp.IsSuccessStatusCode)
            {
                if (meResp.StatusCode == System.Net.HttpStatusCode.Unauthorized || meResp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Token = null;
                    await _storage.RemoveTokenAsync();
                    IsRestored = true;
                    return false;
                }
                IsRestored = true;
                return false;
            }

            CurrentUser = await meResp.Content.ReadFromJsonAsync<CampusEventManager.Web.Models.UserProfile>();
            IsRestored = true;
            OnChange?.Invoke();
            return CurrentUser is not null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch
        {
            IsRestored = true;
            return false;
        }
    }
}
