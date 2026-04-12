using Microsoft.Extensions.Configuration;
using Sts.Domain.Content;
using Sts.Domain.Content.DataSources;

namespace Sts.Infrastructure.DataSources;

/// <summary>
/// Implémentation filesystem de <see cref="IImageDataSource"/>.
/// Stocke les images dans <c>{ContentRootPath}/images/</c> et retourne des URLs complètes.
/// </summary>
public sealed class FileSystemImageDataSource : IImageDataSource
{
    private static readonly HashSet<string> _allowedExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp"];

    private readonly string _basePath;
    private readonly string _baseUrl;

    public FileSystemImageDataSource(IConfiguration config)
    {
        _basePath = config["Images:StoragePath"]
                    ?? throw new InvalidOperationException("La clé Images:StoragePath est manquante.");
        _baseUrl = config["Images:BaseUrl"]
                    ?? throw new InvalidOperationException("La clé Images:BaseUrl est manquante.");
        Directory.CreateDirectory(_basePath);
    }

    /// <inheritdoc/>
    public async Task<string> SaveAsync(string fileName, Stream stream)
    {
        var filePath = Path.Combine(_basePath, fileName);
        await using var dest = File.Create(filePath);
        await stream.CopyToAsync(dest);
        return $"{_baseUrl.TrimEnd('/')}/{fileName}";
    }

    /// <inheritdoc/>
    public Task<List<ImageInfo>> GetAllAsync()
    {
        if (!Directory.Exists(_basePath))
            return Task.FromResult(new List<ImageInfo>());

        var images = Directory.GetFiles(_basePath)
            .Where(f => _allowedExtensions.Contains(
                Path.GetExtension(f).ToLowerInvariant()))
            .Select(f =>
            {
                var name = Path.GetFileName(f);
                return new ImageInfo(
                    FileName: name,
                    Url: $"{_baseUrl.TrimEnd('/')}/{name}",
                    SizeKb: (int)(new FileInfo(f).Length / 1024));
            })
            .OrderByDescending(i =>
                File.GetCreationTime(Path.Combine(_basePath, i.FileName)))
            .ToList();

        return Task.FromResult(images);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(string fileName)
    {
        var filePath = Path.Combine(_basePath, fileName);
        if (!File.Exists(filePath)) return Task.FromResult(false);
        File.Delete(filePath);
        return Task.FromResult(true);
    }
}
