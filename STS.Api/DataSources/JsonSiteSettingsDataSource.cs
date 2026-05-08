using Sts.Domain.Content.DataSources;
using Sts.Domain.Content.Models;
using System.Text.Json;


/// <summary>
/// Implémentation fichier de <see cref="ISiteSettingsDataSource"/>.
/// Lit et écrit <c>siteSettings.json</c> depuis <see cref="IWebHostEnvironment.ContentRootPath"/>.
/// </summary>
public sealed class JsonSiteSettingsDataSource : ISiteSettingsDataSource
{

    private static readonly JsonSerializerOptions _readOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions _writeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _filePath;

    /// <summary>Initialise la source en résolvant le chemin vers <c>siteSettings.json</c>.</summary>
    public JsonSiteSettingsDataSource(string filePath)
    {
        _filePath = filePath;
    }

    /// <inheritdoc/>
    public async Task<SiteSettings> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return new SiteSettings();

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<SiteSettings>(json, _readOptions) ?? new SiteSettings();
    }

    /// <inheritdoc/>
    public async Task SaveAsync(SiteSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(settings, _writeOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
