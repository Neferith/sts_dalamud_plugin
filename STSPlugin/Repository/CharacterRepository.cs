using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Sts.Domain;
using Sts.Domain.Character;

namespace STSPlugin.Repository;

/// <summary>
/// Contrat d'accès aux fiches personnages.
/// </summary>
public interface CharacterRepository
{
    /// <summary>Retourne tous les personnages sauvegardés.</summary>
    IReadOnlyList<Character> GetAll();

    /// <summary>Retourne un personnage par son identifiant, ou null s'il n'existe pas.</summary>
    Character? GetById(Guid id);

    /// <summary>Sauvegarde un personnage (création ou mise à jour).</summary>
    void Save(Character character);

    /// <summary>Supprime un personnage par son identifiant.</summary>
    void Delete(Guid id);
}

/// <summary>
/// Implémentation par défaut : un fichier JSON par personnage dans le dossier de config du plugin.
/// Nom de fichier : {id}.json
/// </summary>
public class DefaultCharacterRepository : CharacterRepository
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
    public DefaultCharacterRepository(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    /// <inheritdoc/>
    public IReadOnlyList<Character> GetAll()
    {
        var characters = new List<Character>();

        foreach (var file in Directory.GetFiles(_directory, "*.json"))
        {
            var character = LoadFile(file);
            if (character != null)
                characters.Add(character);
        }

        return characters;
    }

    /// <inheritdoc/>
    public Character? GetById(Guid id)
    {
        var path = FilePath(id);
        return File.Exists(path) ? LoadFile(path) : null;
    }

    /// <inheritdoc/>
    public void Save(Character character)
    {
        var json = JsonSerializer.Serialize(character, JsonOptions);
        File.WriteAllText(FilePath(character.Id), json);
    }

    /// <inheritdoc/>
    public void Delete(Guid id)
    {
        var path = FilePath(id);
        if (File.Exists(path))
            File.Delete(path);
    }

    // --- privé ---

    private string FilePath(Guid id) => Path.Combine(_directory, $"{id}.json");

    private static Character? LoadFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Character>(json, JsonOptions);
        }
        catch
        {
            // Fichier corrompu ou illisible — on l'ignore silencieusement
            return null;
        }
    }
}
