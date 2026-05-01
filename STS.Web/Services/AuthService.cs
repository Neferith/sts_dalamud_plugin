using System.Net.Http.Json;
using System.Text.Json;

namespace STS.Web.Services;

/// <summary>
/// Service d'authentification pour STS.Web.
/// Stocke le JWT et le nom d'utilisateur en mémoire.
/// </summary>
public class AuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;

    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public bool IsAuthenticated => Token is not null;

    /// <summary>Déclenché à chaque changement d'état (login/logout).</summary>
    public event Action? OnAuthChanged;

    /// <summary>Déclenché quand un composant demande l'ouverture de la modale de connexion.</summary>
    public event Action? OnLoginRequested;

    public AuthService(HttpClient http) => _http = http;

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
            Username = username;
            OnAuthChanged?.Invoke();
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
        _http.DefaultRequestHeaders.Authorization = null;
        OnAuthChanged?.Invoke();
    }

    /// <summary>Demande l'ouverture de la modale de connexion.</summary>
    public void RequestLogin() => OnLoginRequested?.Invoke();

    private sealed record LoginResponse(string Token);
}
