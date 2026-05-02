using System.Net.Http.Json;
using System.Text.Json;
using Sts.Domain.Character;
using Sts.Domain;

namespace STS.Web.Services;

/// <summary>
/// Service d'accès aux fiches personnages via l'API REST.
/// </summary>
public class CharacterApiService(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Retourne toutes les fiches visibles par l'utilisateur connecté.</summary>
    public async Task<IReadOnlyList<Character>> GetAllAsync()
        => await http.GetFromJsonAsync<List<Character>>("/api/characters", JsonOptions)
           ?? [];

    /// <summary>Retourne une fiche par son identifiant.</summary>
    public async Task<Character?> GetByIdAsync(Guid id)
    {
        try
        {
            return await http.GetFromJsonAsync<Character>($"/api/characters/{id}", JsonOptions);
        }
        catch { return null; }
    }

    /// <summary>
    /// Crée un nouveau personnage.
    /// Retourne le personnage créé, ou null + message d'erreur.
    /// </summary>
    public async Task<(Character? character, string? error)> CreateAsync(string name, RankKey rank)
    {
        var response = await http.PostAsJsonAsync("/api/characters",
            new { name, rank = (int)rank });

        if (response.IsSuccessStatusCode)
        {
            var character = await response.Content
                .ReadFromJsonAsync<Character>(JsonOptions);
            return (character, null);
        }

        var body = await response.Content.ReadAsStringAsync();
        return (null, string.IsNullOrWhiteSpace(body)
            ? $"Erreur {(int)response.StatusCode}"
            : body.Trim('"'));
    }

    /// <summary>
    /// Met à jour un personnage existant.
    /// Retourne null si succès, message d'erreur sinon.
    /// </summary>
    public async Task<string?> UpdateAsync(Character character)
    {
        var response = await http.PutAsJsonAsync(
            $"/api/characters/{character.Id}", character);

        if (response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(body)
            ? $"Erreur {(int)response.StatusCode}"
            : body.Trim('"');
    }

    /// <summary>
    /// Supprime un personnage.
    /// Retourne null si succès, message d'erreur sinon.
    /// </summary>
    public async Task<string?> DeleteAsync(Guid id)
    {
        var response = await http.DeleteAsync($"/api/characters/{id}");
        if (response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(body)
            ? $"Erreur {(int)response.StatusCode}"
            : body.Trim('"');
    }
}
