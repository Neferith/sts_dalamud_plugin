namespace Sts.Domain.Character;

/// <summary>Uploade l'image d'un personnage et met à jour son champ <see cref="Character.ImageUrl"/>.</summary>
public interface IUploadCharacterImageUseCase
{
    /// <summary>Exécute l'upload.</summary>
    /// <param name="characterId">Identifiant du personnage cible.</param>
    /// <param name="stream">Flux binaire du fichier image.</param>
    /// <param name="fileName">Nom du fichier original, utilisé pour déduire l'extension.</param>
    /// <returns>
    /// L'URL relative publique de l'image en cas de succès,
    /// ou un message d'erreur si l'opération échoue.
    /// </returns>
    Task<(string? ImageUrl, string? Error)> ExecuteAsync(Guid characterId, Stream stream, string fileName);
}


/// <inheritdoc/>
public sealed class UploadCharacterImageUseCase(
    ICharacterRepository characters,
    string uploadDir) : IUploadCharacterImageUseCase
{
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    /// <inheritdoc/>
    public async Task<(string? ImageUrl, string? Error)> ExecuteAsync(
        Guid characterId, Stream stream, string fileName)
    {
        var character = await characters.GetByIdAsync(characterId);
        if (character is null) return (null, "Personnage introuvable.");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return (null, "Format non supporté. Utilisez JPG, PNG ou WebP.");

        Directory.CreateDirectory(uploadDir);

        foreach (var existing in Directory.GetFiles(uploadDir, $"{characterId}.*"))
            File.Delete(existing);

        var filePath = Path.Combine(uploadDir, $"{characterId}{ext}");
        await using var fs = File.Create(filePath);
        await stream.CopyToAsync(fs);

        character.ImageUrl = $"/api/characters/{characterId}/image";
        await characters.SaveAsync(character);

        return (character.ImageUrl, null);
    }
}
