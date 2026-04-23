using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sts.Discord;

/// <summary>
/// Persiste les correspondances entre les entités STS et les objets Discord
/// (Forum channels, threads) dans un fichier JSON local.
/// </summary>
/// <remarks>
/// Structure du fichier :
/// <code>
/// {
///   "sections": { "guide-systeme": "1234567890123456789" },
///   "posts":    { "systeme-tres-simple": "9876543210987654321" }
/// }
/// </code>
/// La clé <c>sections</c> associe un <c>RulesSection.Id</c> à l'ID du Forum Channel Discord.
/// La clé <c>posts</c> associe un <c>RulesPost.Id</c> à l'ID du thread Discord.
/// Dans un Forum Discord, le thread et son message d'ouverture partagent le même ID.
/// </remarks>
public sealed class DiscordMappingStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private MappingData _data = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <param name="filePath">Chemin absolu vers le fichier <c>discord-mappings.json</c>.</param>
    public DiscordMappingStore(string filePath)
    {
        _filePath = filePath;
    }

    // ─── Chargement ──────────────────────────────────────────────────────────

    /// <summary>Charge le fichier de mappings depuis le disque.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
        {
            _data = new MappingData();
            return;
        }

        await _lock.WaitAsync(ct);
        try
        {
            await using var stream = File.OpenRead(_filePath);
            _data = await JsonSerializer.DeserializeAsync<MappingData>(stream, JsonOptions, ct)
                    ?? new MappingData();
        }
        finally
        {
            _lock.Release();
        }
    }

    // ─── Sections ────────────────────────────────────────────────────────────

    /// <summary>
    /// Retourne l'ID du Forum Channel Discord associé à la section,
    /// ou <see langword="null"/> si aucun n'est configuré.
    /// </summary>
    public ulong? GetForumChannelId(string sectionId)
        => _data.Sections.TryGetValue(sectionId, out var raw)
            && ulong.TryParse(raw, out var id) ? id : null;

    /// <summary>Associe un Forum Channel Discord à une section STS.</summary>
    public void SetForumChannelId(string sectionId, ulong channelId)
        => _data.Sections[sectionId] = channelId.ToString();

    /// <summary>Retourne tous les mappings section → Forum Channel sous forme de dictionnaire.</summary>
    public IReadOnlyDictionary<string, string> GetAllSectionMappings()
        => _data.Sections;

    /// <summary>Supprime le mapping Forum Channel d'une section.</summary>
    public void RemoveSectionMapping(string sectionId)
        => _data.Sections.Remove(sectionId);

    // ─── Posts ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Retourne l'ID du thread Discord associé au post,
    /// ou <see langword="null"/> si le post n'a jamais été publié.
    /// </summary>
    public ulong? GetThreadId(string postId)
        => _data.Posts.TryGetValue(postId, out var raw)
            && ulong.TryParse(raw, out var id) ? id : null;

    /// <summary>Associe un thread Discord à un post STS.</summary>
    public void SetThreadId(string postId, ulong threadId)
        => _data.Posts[postId] = threadId.ToString();

    /// <summary>Supprime le mapping d'un post (ex : après archivage).</summary>
    public void RemovePost(string postId)
        => _data.Posts.Remove(postId);


    // ─── Persistance ─────────────────────────────────────────────────────────

    /// <summary>Écrit les mappings sur le disque.</summary>
    public async Task SaveAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, _data, JsonOptions, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    // ─── Modèle interne ──────────────────────────────────────────────────────

    private sealed class MappingData
    {
        [JsonPropertyName("sections")]
        public Dictionary<string, string> Sections { get; set; } = [];

        [JsonPropertyName("posts")]
        public Dictionary<string, string> Posts { get; set; } = [];
    }
}
