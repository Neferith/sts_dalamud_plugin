using System.Net.Http.Json;
using System.Text.Json;
using Sts.Domain.Character;
using Sts.Domain;
using System.Net.Http.Headers; // à ajouter en haut

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

    /// <summary>
    /// Uploade l'image d'un personnage.
    /// Retourne null si succès, message d'erreur sinon.
    /// </summary>
    public async Task<string?> UploadImageAsync(Guid id, Stream stream, string fileName, string contentType)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);

        var response = await http.PostAsync($"/api/characters/{id}/image", content);
        if (response.IsSuccessStatusCode) return null;

        var body = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(body)
            ? $"Erreur {(int)response.StatusCode}"
            : body.Trim('"');
    }

    /// <summary>Télécharge l'export Discord (ZIP) d'un personnage.</summary>
    public async Task<(byte[]? Data, string? Error)> DownloadDiscordExportAsync(Guid id)
    {
        var response = await http.GetAsync($"/api/characters/{id}/export/discord");
        if (!response.IsSuccessStatusCode)
            return (null, $"Erreur {(int)response.StatusCode}");
        return (await response.Content.ReadAsByteArrayAsync(), null);
    }

    /// <summary>Télécharge l'export PDF d'un personnage.</summary>
    public async Task<(byte[]? Data, string? Error)> DownloadPdfAsync(Guid id)
    {
        var response = await http.GetAsync($"/api/characters/{id}/export/pdf");
        if (!response.IsSuccessStatusCode)
            return (null, $"Erreur {(int)response.StatusCode}");
        return (await response.Content.ReadAsByteArrayAsync(), null);
    }

    /// <summary>Construit l'URL absolue d'une URL relative renvoyée par l'API.</summary>
    public string AbsoluteImageUrl(string relativeUrl)
        => new Uri(http.BaseAddress!, relativeUrl).ToString();
}
