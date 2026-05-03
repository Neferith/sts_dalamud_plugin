using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace STS.Web.Services;

/// <summary>
/// Service d'authentification pour STS.Web.
/// Stocke le JWT et les claims en mémoire.
/// </summary>
public class AuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public string? Role { get; private set; }
    public Guid? UserId { get; private set; }
    public bool IsAuthenticated => Token is not null;
    public bool IsAdmin => Role == "admin";
    public int MaxCharacters => IsAdmin ? 8 : 1;

    /// <summary>Déclenché à chaque changement d'état (login/logout).</summary>
    public event Action? OnAuthChanged;

    /// <summary>Déclenché quand un composant demande l'ouverture de la modale de connexion.</summary>
    public event Action? OnLoginRequested;

    public AuthService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    /// <summary>
    /// Tente de se connecter avec les identifiants fournis.
    /// Retourne null si succès, message d'erreur sinon.
    /// </summary>
    public async Task<string?> LoginAsync(string username, string password)
    {
        try
        {
            _http.DefaultRequestHeaders.Authorization = null;
            var response = await _http.PostAsJsonAsync("/api/auth/login",
                new { username, password });

            if (!response.IsSuccessStatusCode)
                return "Identifiants incorrects.";

            var result = await response.Content
                .ReadFromJsonAsync<LoginResponse>(JsonOptions);

            if (result?.Token is null)
                return "Réponse inattendue du serveur.";

            Token = result.Token;
            ParseToken(result.Token);
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Token);

            OnAuthChanged?.Invoke();
            await _js.InvokeVoidAsync("localStorage.setItem", "sts_token", Token);
            return null;
        }
        catch
        {
            return "Impossible de contacter le serveur.";
        }
    }

    /// <summary>Déconnecte l'utilisateur courant.</summary>
    public void Logout()
    {
        Token = null;
        Username = null;
        Role = null;
        UserId = null;
        _http.DefaultRequestHeaders.Authorization = null;
        _ = _js.InvokeVoidAsync("localStorage.removeItem", "sts_token");
        OnAuthChanged?.Invoke();
    }

    /// <summary>Demande l'ouverture de la modale de connexion depuis n'importe quel composant.</summary>
    public void RequestLogin() => OnLoginRequested?.Invoke();

    // ── Privé ─────────────────────────────────────────────────────────────────

    /// <summary>Parse le JWT pour extraire username, role et userId.</summary>
    private void ParseToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return;

            var payload = parts[1];
            // Padding base64url
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (claims is null) return;

            if (claims.TryGetValue(ClaimTypes.Name, out var name))
                Username = name.GetString();
            else if (claims.TryGetValue("unique_name", out var uname))
                Username = uname.GetString();

            if (claims.TryGetValue(ClaimTypes.Role, out var role))
                Role = role.GetString();
            else if (claims.TryGetValue("role", out var r))
                Role = r.GetString();

            if (claims.TryGetValue(ClaimTypes.NameIdentifier, out var sub))
            {
                if (Guid.TryParse(sub.GetString(), out var id)) UserId = id;
            }
            else if (claims.TryGetValue("nameid", out var nameid))
            {
                if (Guid.TryParse(nameid.GetString(), out var id)) UserId = id;
            }
        }
        catch { /* Token malformé — on ignore */ }
    }

    private sealed record LoginResponse(string Token);

    /// <summary>Tente de restaurer la session depuis le localStorage.</summary>
    public async Task TryRestoreSessionAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", "sts_token");
            if (string.IsNullOrWhiteSpace(token)) return;

            Token = token;
            ParseToken(token);
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Token);

            OnAuthChanged?.Invoke();
        }
        catch { /* JS non disponible ou token invalide */ }
    }
}
