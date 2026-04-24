using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Sts.Admin.Models;

namespace Sts.Admin.Services;

/// <summary>
/// Client HTTP wrappé qui injecte le Bearer token sur chaque requête
/// et redirige vers /login en cas de 401.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;
    private readonly NavigationManager _nav;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ApiClient(HttpClient http, AuthService auth, NavigationManager nav)
    {
        _http = http;
        _auth = auth;
        _nav  = nav;
    }

    // ─── Auth ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Endpoint de login — n'envoie pas de token (endpoint public).
    /// Retourne null si les credentials sont incorrects.
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        _http.DefaultRequestHeaders.Authorization = null;
        var response = await _http.PostAsJsonAsync("/api/auth/login", new { username, password });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
    }

    // ─── GET ──────────────────────────────────────────────────────────────────

    public async Task<T?> GetAsync<T>(string url)
    {
        SetAuthHeader();
        var response = await _http.GetAsync(url);
        if (HandleUnauthorized(response)) return default;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    // ─── POST ─────────────────────────────────────────────────────────────────

    public async Task<(T? data, string? error)> PostAsync<T>(string url, object body)
    {
        SetAuthHeader();
        var response = await _http.PostAsJsonAsync(url, body);
        if (HandleUnauthorized(response)) return (default, "Non autorisé.");
        if (!response.IsSuccessStatusCode)
            return (default, await ReadError(response));
        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions), null);
    }

    public async Task<string?> PostNoContentAsync(string url, object body)
    {
        SetAuthHeader();
        var response = await _http.PostAsJsonAsync(url, body);
        if (HandleUnauthorized(response)) return "Non autorisé.";
        if (!response.IsSuccessStatusCode)
            return await ReadError(response);
        return null;
    }

    // ─── PUT ──────────────────────────────────────────────────────────────────

    public async Task<(T? data, string? error)> PutAsync<T>(string url, object body)
    {
        SetAuthHeader();
        var response = await _http.PutAsJsonAsync(url, body);
        if (HandleUnauthorized(response)) return (default, "Non autorisé.");
        if (!response.IsSuccessStatusCode)
            return (default, await ReadError(response));

        // 204 NoContent — pas de corps à désérialiser
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return (default, null);

        return (await response.Content.ReadFromJsonAsync<T>(JsonOptions), null);
    }

    // ─── DELETE ───────────────────────────────────────────────────────────────

    /// <returns>null si succès, message d'erreur sinon.</returns>
    public async Task<string?> DeleteAsync(string url)
    {
        SetAuthHeader();
        var response = await _http.DeleteAsync(url);
        if (HandleUnauthorized(response)) return "Non autorisé.";
        if (!response.IsSuccessStatusCode)
            return await ReadError(response);
        return null;
    }

    // ─── Privé ────────────────────────────────────────────────────────────────

    public void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Authorization = _auth.Token is not null
            ? new AuthenticationHeaderValue("Bearer", _auth.Token)
            : null;
    }

    /// <returns>True si 401 (redirige vers login).</returns>
    private bool HandleUnauthorized(HttpResponseMessage response)
    {
        if (response.StatusCode != HttpStatusCode.Unauthorized) return false;
        _nav.NavigateTo("/login");
        return true;
    }

    private static async Task<string> ReadError(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(body)
            ? $"Erreur {(int)response.StatusCode}"
            : body.Trim('"');
    }

    public async Task<(JsonElement? data, string? error)> PostFormAsync(string url, HttpContent content)
    {
        SetAuthHeader();
        var response = await _http.PostAsync(url, content);
        if (HandleUnauthorized(response)) return (null, "Non autorisé.");
        if (!response.IsSuccessStatusCode)
            return (null, await ReadError(response));

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return (json, null);
    }
}
