using System.IO;
using System.Text.Json;

namespace STSPlugin.DataSource;

/// <summary>
/// Contrat d'accès à la source de données brute.
/// </summary>
public interface IDataSource
{
    /// <summary>
    /// Charge et retourne le modèle de données complet.
    /// </summary>
    DataModel Load();
}

/// <summary>
/// Implémentation locale : lit un fichier data.json sur le disque.
/// </summary>
public class LocalJsonDataSource : IDataSource
{
    private readonly string _filePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Initialise la source avec le chemin absolu vers data.json.
    /// </summary>
    /// <param name="filePath">Chemin absolu vers le fichier data.json.</param>
    public LocalJsonDataSource(string filePath)
        => _filePath = filePath;

    /// <inheritdoc/>
    public DataModel Load()
    {
        if (!File.Exists(_filePath))
            return new DataModel();

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<DataModel>(json, JsonOptions) ?? new DataModel();
    }
}
