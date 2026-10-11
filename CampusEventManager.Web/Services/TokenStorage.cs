using Microsoft.JSInterop;

namespace CampusEventManager.Web.Services;

public class TokenStorage
{
    private readonly IJSRuntime _js;
    public TokenStorage(IJSRuntime js) => _js = js;

    public ValueTask SetTokenAsync(string token) => _js.InvokeVoidAsync("localStorage.setItem", "cem_token", token);
    public ValueTask<string?> GetTokenAsync() => _js.InvokeAsync<string?>("localStorage.getItem", "cem_token");
    public ValueTask RemoveTokenAsync() => _js.InvokeVoidAsync("localStorage.removeItem", "cem_token");
}
