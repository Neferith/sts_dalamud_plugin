using Microsoft.JSInterop;

namespace Sts.Admin.Services;

public class AuthService
{
    private readonly IJSRuntime _js;
    private string? _token;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private const string StorageKey = "sts_jwt";

    public AuthService(IJSRuntime js) => _js = js;

    // Vérifie token non-null ET non expiré
    public bool IsAuthenticated => !string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry;

    public string? Token => _token;
    public string? Role { get; private set; }
    public bool IsAdmin => Role == "admin";
    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            var raw = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (raw is not null && ParseToken(raw))
            {
                // Token valide et non expiré — on le garde
                _token = raw;
            }
            else if (raw is not null)
            {
                // Token présent mais expiré/invalide — on nettoie d'emblée
                await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            }
        }
        catch { }
        finally { IsInitialized = true; }
    }

    public async Task SetTokenAsync(string token)
    {
        if (!ParseToken(token)) return; // token malformé, on refuse
        _token = token;
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, token);
    }

    public async Task LogoutAsync()
    {
        ClearToken();
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    /// <summary>
    /// Purge le token en mémoire de façon synchrone (pour le handler 401).
    /// Le retrait du localStorage est lancé en fire-and-forget.
    /// </summary>
    public void ClearToken()
    {
        _token = null;
        _tokenExpiry = DateTime.MinValue;
        Role = null;
        _ = _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    /// <returns>True si le token est valide et non expiré.</returns>
    private bool ParseToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return false;

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = (payload.Length % 4) switch
            {
                2 => payload + "==",
                3 => payload + "=",
                _ => payload
            };

            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var claims = System.Text.Json.JsonSerializer
                .Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);
            if (claims is null) return false;

            // Expiry
            if (claims.TryGetValue("exp", out var exp))
            {
                var expUnix = exp.GetInt64();
                _tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                if (DateTime.UtcNow >= _tokenExpiry) return false; // expiré
            }

            // Role
            if (claims.TryGetValue("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var role))
                Role = role.GetString();
            else if (claims.TryGetValue("role", out var r))
                Role = r.GetString();

            return true;
        }
        catch { return false; }
    }
}
