using Sts.Domain.Character;
using STSPlugin.Auth;
using STSPlugin.UseCases.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace STSPlugin.Repository;

/// <summary>
/// Implémentation distante de <see cref="ICharacterRepository"/>.
/// Appelle l'API STS via HTTP en injectant le JWT obtenu via <see cref="IGetTokenUseCase"/>.
/// </summary>
public class RemoteCharacterRepository : ICharacterRepository
{
    private readonly string _baseUrl;
    private readonly IGetTokenUseCase _getToken;
    private readonly AuthState _authState;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <param name="baseUrl">URL de base de l'API (ex : https://api.nlrp.fr). Sans slash final.</param>
    /// <param name="getToken">Use case fournissant un JWT valide.</param>
    /// <param name="authState">État d'authentification — fournit le UserId pour le filtrage.</param>
    public RemoteCharacterRepository(string baseUrl, IGetTokenUseCase getToken, AuthState authState)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _getToken = getToken;
        _authState = authState;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Character>> GetAllAsync()
    {
        var url = $"{_baseUrl}/api/characters";
        Plugin.Log.Debug("[STS] RemoteCharacterRepository.GetAllAsync — GET {0}", url);
        using var client = await MakeClientAsync();
        var result = await client.GetFromJsonAsync<List<Character>>(url, JsonOptions);

        var all = result ?? [];

        // Filtrer sur le UserId du joueur connecté — le plugin n'affiche que ses propres fiches
        if (_authState.UserId.HasValue)
        {
            var filtered = all.Where(c => c.UserId == _authState.UserId.Value).ToList();
            Plugin.Log.Debug("[STS] RemoteCharacterRepository.GetAllAsync — {0}/{1} fiche(s) après filtre UserId",
                filtered.Count, all.Count);
            return filtered;
        }

        Plugin.Log.Debug("[STS] RemoteCharacterRepository.GetAllAsync — {0} fiche(s) (pas de filtre UserId)", all.Count);
        return all;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Character>> GetByUserIdAsync(Guid userId)
    {
        // L'API retourne déjà uniquement les fiches de l'utilisateur connecté
        // pour les membres — GetAllAsync suffit.
        return await GetAllAsync();
    }

    /// <inheritdoc/>
    public async Task<Character?> GetByIdAsync(Guid id)
    {
        using var client = await MakeClientAsync();
        try
        {
            return await client.GetFromJsonAsync<Character>(
                $"{_baseUrl}/api/characters/{id}", JsonOptions);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Character character)
    {
        using var client = await MakeClientAsync();

        // Détermine si c'est une création ou une mise à jour
        // en vérifiant si la fiche existe déjà côté API
        var existing = await GetByIdAsync(character.Id);

        if (existing is null)
        {
            // Création — POST avec le body minimal attendu par l'API
            await client.PostAsJsonAsync(
                $"{_baseUrl}/api/characters",
                new { name = character.Name, rank = (int)character.RankKey },
                JsonOptions);
        }
        else
        {
            // Mise à jour — PUT avec la fiche complète
            await client.PutAsJsonAsync(
                $"{_baseUrl}/api/characters/{character.Id}",
                character,
                JsonOptions);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id)
    {
        using var client = await MakeClientAsync();
        await client.DeleteAsync($"{_baseUrl}/api/characters/{id}");
    }

    // ── Privé ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crée un <see cref="HttpClient"/> avec le JWT injecté en Bearer.
    /// </summary>
    private async Task<HttpClient> MakeClientAsync()
    {
        var token = await _getToken.ExecuteAsync();
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        if (token is not null)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
