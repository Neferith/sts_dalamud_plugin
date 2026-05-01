using Sts.Domain.Character;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace STSPlugin.Repository;

/// <summary>
/// Implémentation locale de <see cref="ICharacterRepository"/>.
/// Stocke chaque personnage dans un fichier JSON séparé nommé <c>{id}.json</c>
/// dans le dossier de configuration du plugin.
/// </summary>
public class LocalCharacterRepository : ICharacterRepository
{
    private readonly string _directory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Initialise le repository avec le dossier de stockage.
    /// Le dossier est créé s'il n'existe pas.
    /// </summary>
    /// <param name="directory">Chemin absolu du dossier de stockage des fiches.</param>
    public LocalCharacterRepository(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Character>> GetAllAsync()
    {
        var characters = new List<Character>();

        foreach (var file in Directory.GetFiles(_directory, "*.json"))
        {
            var character = await LoadFileAsync(file);
            if (character != null)
                characters.Add(character);
        }

        return [.. characters.OrderBy(c => c.Name)];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Character>> GetByPlayerIdAsync(Guid playerId)
    {
        var all = await GetAllAsync();
        return [.. all.Where(c => c.PlayerId == playerId)];
    }

    /// <inheritdoc/>
    public async Task<Character?> GetByIdAsync(Guid id)
    {
        var path = FilePath(id);
        return File.Exists(path) ? await LoadFileAsync(path) : null;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Character character)
    {
        var json = JsonSerializer.Serialize(character, JsonOptions);
        await File.WriteAllTextAsync(FilePath(character.Id), json);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(Guid id)
    {
        var path = FilePath(id);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    // ── Privé ─────────────────────────────────────────────────────────────────

    private string FilePath(Guid id) => Path.Combine(_directory, $"{id}.json");

    private async Task<Character?> LoadFileAsync(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<Character>(json, JsonOptions);
        }
        catch
        {
            // Fichier corrompu ou illisible — ignoré silencieusement
            return null;
        }
    }
}
