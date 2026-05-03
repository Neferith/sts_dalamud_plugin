using Microsoft.JSInterop;

namespace Sts.Admin.Services;

/// <summary>
/// Gère l'état d'authentification côté Blazor WASM.
/// Le token JWT est persisté dans localStorage pour survivre aux rechargements.
/// </summary>
public class AuthService
{
    private readonly IJSRuntime _js;
    private string? _token;

    private const string StorageKey = "sts_jwt";

    public AuthService(IJSRuntime js) => _js = js;

    /// <summary>True si l'utilisateur possède un token JWT valide en mémoire.</summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    /// <summary>Token JWT courant, ou null si non authentifié.</summary>
    public string? Token => _token;

    public string? Role { get; private set; }
    public bool IsAdmin => Role == "admin";

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Charge le token depuis localStorage.
    /// Doit être appelé une fois au démarrage dans App.razor.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _token = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (_token is not null) ParseToken(_token);
        }
        catch { }
        finally { IsInitialized = true; }
    }

    /// <summary>Stocke le token en mémoire et dans localStorage.</summary>
    public async Task SetTokenAsync(string token)
    {
        _token = token;
        ParseToken(token);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, token);
    }

    /// <summary>Efface le token de la mémoire et de localStorage.</summary>
    public async Task LogoutAsync()
    {
        _token = null;
        Role = null;
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    private void ParseToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var claims = System.Text.Json.JsonSerializer
                .Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);
            if (claims is null) return;

            if (claims.TryGetValue("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var role))
                Role = role.GetString();
            else if (claims.TryGetValue("role", out var r))
                Role = r.GetString();
        }
        catch { }
    }
}
