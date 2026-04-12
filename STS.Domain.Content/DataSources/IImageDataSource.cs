namespace Sts.Domain.Content.DataSources;

/// <summary>Contrat d'accès brut au stockage des images.</summary>
public interface IImageDataSource
{
    /// <summary>
    /// Sauvegarde un fichier et retourne son URL publique complète.
    /// </summary>
    Task<string> SaveAsync(string fileName, Stream stream);

    /// <summary>Retourne la liste de toutes les images stockées.</summary>
    Task<List<ImageInfo>> GetAllAsync();

    /// <summary>Supprime une image par son nom de fichier.</summary>
    Task<bool> DeleteAsync(string fileName);
}
