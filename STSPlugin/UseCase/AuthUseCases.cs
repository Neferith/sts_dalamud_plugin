using STSPlugin.Auth;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace STSPlugin.UseCases.Auth;

// ── ILoginUseCase ─────────────────────────────────────────────────────────────

/// <summary>
/// Cas d'usage : authentifier le joueur auprès de l'API STS.
/// Stocke le JWT dans <see cref="AuthState"/> en cas de succès.
/// </summary>
public interface ILoginUseCase
{
    /// <summary>
    /// Tente de se connecter avec les identifiants fournis.
    /// </summary>
    /// <param name="username">Nom d'utilisateur.</param>
    /// <param name="password">Mot de passe en clair.</param>
    /// <returns>Null si succès, message d'erreur sinon.</returns>
    Task<string?> ExecuteAsync(string username, string password);
}

/// <summary>Implémentation par défaut de <see cref="ILoginUseCase"/>.</summary>
public class LoginUseCase : ILoginUseCase
{
    private readonly AuthState _state;
    private readonly Configuration _config;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <param name="state">État d'authentification partagé.</param>
    /// <param name="config">Configuration du plugin (pour AuthUrl).</param>
    public LoginUseCase(AuthState state, Configuration config)
    {
        _state = state;
        _config = config;
    }

    /// <inheritdoc/>
    public async Task<string?> ExecuteAsync(string username, string password)
    {
        _state.LastError = null;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = await client.PostAsJsonAsync(
                _config.AuthUrl,
                new { username, password });

            if (!response.IsSuccessStatusCode)
            {
                _state.LastError = "Identifiants incorrects.";
                _state.Notify();
                return _state.LastError;
            }

            var result = await response.Content
                .ReadFromJsonAsync<LoginResponse>(JsonOptions);

            if (result?.Token is null)
            {
                _state.LastError = "Réponse inattendue du serveur.";
                _state.Notify();
                return _state.LastError;
            }

            _state.Token = result.Token;
            _state.TokenExpiry = DateTime.UtcNow.AddHours(7.5); // marge de 30 min
            _state.Username = username;
            _state.UserId = ParseUserIdFromToken(result.Token);
            _state.Notify();
            return null;
        }
        catch (Exception ex)
        {
            _state.LastError = $"Erreur réseau : {ex.Message}";
            _state.Notify();
            return _state.LastError;
        }
    }

    private sealed record LoginResponse(string Token);

    /// <summary>Parse le UserId (sub claim) depuis le payload JWT.</summary>
    private static Guid? ParseUserIdFromToken(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3) return null;

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var claims = System.Text.Json.JsonSerializer
                .Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);
            if (claims is null) return null;

            // ClaimTypes.NameIdentifier sérialisé en "nameid" ou en URI complet
            foreach (var key in new[] { "nameid", "sub",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" })
            {
                if (claims.TryGetValue(key, out var el) &&
                    Guid.TryParse(el.GetString(), out var id))
                    return id;
            }
            return null;
        }
        catch { return null; }
    }
}

// ── ILogoutUseCase ────────────────────────────────────────────────────────────

/// <summary>Cas d'usage : déconnecter le joueur — efface le token en mémoire.</summary>
public interface ILogoutUseCase
{
    /// <summary>Efface le token et les informations de session.</summary>
    void Execute();
}

/// <summary>Implémentation par défaut de <see cref="ILogoutUseCase"/>.</summary>
public class LogoutUseCase : ILogoutUseCase
{
    private readonly AuthState _state;

    /// <param name="state">État d'authentification partagé.</param>
    public LogoutUseCase(AuthState state) => _state = state;

    /// <inheritdoc/>
    public void Execute()
    {
        _state.Token = null;
        _state.TokenExpiry = DateTime.MinValue;
        _state.Username = null;
        _state.UserId = null;
        _state.LastError = null;
        _state.Notify();
    }
}

// ── IGetTokenUseCase ──────────────────────────────────────────────────────────

/// <summary>
/// Cas d'usage : obtenir un token JWT valide.
/// Renouvelle automatiquement si expiré, en utilisant les identifiants
/// stockés dans la configuration.
/// </summary>
public interface IGetTokenUseCase
{
    /// <summary>
    /// Retourne le token JWT valide.
    /// Si expiré, tente un renouvellement automatique avec les credentials en config.
    /// </summary>
    /// <returns>Le token JWT, ou null si non connecté ou si le renouvellement échoue.</returns>
    Task<string?> ExecuteAsync();
}

/// <summary>Implémentation par défaut de <see cref="IGetTokenUseCase"/>.</summary>
public class GetTokenUseCase : IGetTokenUseCase
{
    private readonly AuthState _state;
    private readonly Configuration _config;
    private readonly ILoginUseCase _login;

    /// <param name="state">État d'authentification partagé.</param>
    /// <param name="config">Configuration du plugin (pour les credentials).</param>
    /// <param name="login">Use case de connexion.</param>
    public GetTokenUseCase(AuthState state, Configuration config, ILoginUseCase login)
    {
        _state = state;
        _config = config;
        _login = login;
    }

    /// <inheritdoc/>
    public async Task<string?> ExecuteAsync()
    {
        if (_state.IsAuthenticated) return _state.Token;

        // Renouvellement automatique si credentials disponibles
        if (!string.IsNullOrWhiteSpace(_config.PlayerUsername) &&
            !string.IsNullOrWhiteSpace(_config.PlayerPassword))
        {
            await _login.ExecuteAsync(_config.PlayerUsername, _config.PlayerPassword);
        }

        return _state.Token;
    }
}
