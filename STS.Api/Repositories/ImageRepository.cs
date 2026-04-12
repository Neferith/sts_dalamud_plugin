using Sts.Domain.Content;
using Sts.Domain.Content.DataSources;
using Sts.Domain.Content.Repositories;

namespace Sts.Api.Repositories;

/// <summary>
/// Implémentation de <see cref="IImageRepository"/>.
/// Contient la validation métier (taille, extension, path traversal).
/// </summary>
public sealed class ImageRepository(IImageDataSource dataSource) : IImageRepository
{
    private static readonly HashSet<string> _allowedExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 Mo

    /// <inheritdoc/>
    public async Task<(string? url, string? error)> UploadAsync(
        string fileName, Stream stream, long sizeBytes)
    {
        if (sizeBytes == 0)
            return (null, "Fichier vide.");

        if (sizeBytes > MaxFileSizeBytes)
            return (null, "Fichier trop volumineux (max 5 Mo).");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(ext))
            return (null, $"Extension non autorisée. Formats acceptés : {string.Join(", ", _allowedExtensions)}");

        var storedName = $"{Guid.NewGuid():N}{ext}";
        var url = await dataSource.SaveAsync(storedName, stream);
        return (url, null);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ImageInfo>> GetAllAsync() =>
        await dataSource.GetAllAsync();

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string fileName)
    {
        // Protection path traversal
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
            return false;

        return await dataSource.DeleteAsync(fileName);
    }
}
