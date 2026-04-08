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

    /// <summary>
    /// Charge le token depuis localStorage.
    /// Doit être appelé une fois au démarrage dans App.razor.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _token = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        }
        catch
        {
            // JS runtime pas encore disponible (pre-rendering) — on ignore
        }
    }

    /// <summary>Stocke le token en mémoire et dans localStorage.</summary>
    public async Task SetTokenAsync(string token)
    {
        _token = token;
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, token);
    }

    /// <summary>Efface le token de la mémoire et de localStorage.</summary>
    public async Task LogoutAsync()
    {
        _token = null;
        await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }
}
