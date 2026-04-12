namespace Sts.Domain.Content.Repositories;

/// <summary>Contrat métier de gestion des images.</summary>
public interface IImageRepository
{
    /// <summary>
    /// Valide et uploade une image.
    /// Retourne l'URL complète si succès, un message d'erreur sinon.
    /// </summary>
    Task<(string? url, string? error)> UploadAsync(string fileName, Stream stream, long sizeBytes);

    /// <summary>Retourne toutes les images.</summary>
    Task<IReadOnlyList<ImageInfo>> GetAllAsync();

    /// <summary>Supprime une image. Retourne <c>false</c> si introuvable.</summary>
    Task<bool> DeleteAsync(string fileName);
}
