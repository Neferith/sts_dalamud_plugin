using System.Text.Json;
using Sts.Domain.Content;
using Sts.Domain.Content.DataSources;

namespace Sts.Api.DataSources;

/// <summary>
/// Implémentation fichier de <see cref="IRulesDataSource"/>.
/// Lit et écrit <c>rules.json</c> depuis <see cref="IWebHostEnvironment.ContentRootPath"/>.
/// </summary>
public sealed class JsonRulesDataSource : IRulesDataSource
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

    /// <summary>Initialise la source en résolvant le chemin vers <c>rules.json</c>.</summary>
    public JsonRulesDataSource(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "rules.json");
    }

    /// <inheritdoc/>
    public async Task<List<RulesSection>> LoadAsync()
    {
        if (!File.Exists(_filePath))
            return [];

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<RulesSection>>(json, _readOptions) ?? [];
    }

    /// <inheritdoc/>
    public async Task SaveAsync(List<RulesSection> sections)
    {
        var json = JsonSerializer.Serialize(sections, _writeOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
