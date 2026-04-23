using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sts.Discord;

/// <summary>
/// Persiste les correspondances entre les entités STS et les objets Discord
/// (Forum channels, threads, messages) dans un fichier JSON local.
/// </summary>
/// <remarks>
/// Structure du fichier :
/// <code>
/// {
///   "sections": { "guide-systeme": "1234567890123456789" },
///   "posts": {
///     "systeme-tres-simple": {
///       "threadId": "9876543210987654321",
///       "messageIds": ["9876543210987654321", "1111111111111111111"]
///     }
///   }
/// }
/// </code>
/// La clé <c>sections</c> associe un <c>RulesSection.Id</c> à l'ID du Forum Channel Discord.
/// La clé <c>posts</c> associe un <c>RulesPost.Id</c> à son mapping Discord complet.
/// Le premier <c>messageId</c> est toujours égal au <c>threadId</c> (message d'ouverture).
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
            _data = await DeserializeWithMigrationAsync(stream, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Désérialise le fichier en gérant la migration de l'ancien format
    /// (posts : string) vers le nouveau (posts : PostMapping).
    /// </summary>
    private static async Task<MappingData> DeserializeWithMigrationAsync(
        Stream stream, CancellationToken ct)
    {
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var data = new MappingData();

        // ── Sections ─────────────────────────────────────────────────────────
        if (root.TryGetProperty("sections", out var sectionsEl))
        {
            foreach (var prop in sectionsEl.EnumerateObject())
                data.Sections[prop.Name] = prop.Value.GetString() ?? string.Empty;
        }

        // ── Posts (avec migration de l'ancien format string) ─────────────────
        if (root.TryGetProperty("posts", out var postsEl))
        {
            foreach (var prop in postsEl.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    // Ancien format : "postId": "threadId"
                    var threadId = prop.Value.GetString() ?? string.Empty;
                    data.Posts[prop.Name] = new PostMapping
                    {
                        ThreadId = threadId,
                        MessageIds = [threadId],
                    };
                }
                else
                {
                    // Nouveau format : "postId": { "threadId": "...", "messageIds": [...] }
                    data.Posts[prop.Name] = prop.Value.Deserialize<PostMapping>(JsonOptions)
                                           ?? new PostMapping();
                }
            }
        }

        return data;
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

    /// <summary>Retourne tous les mappings section → Forum Channel.</summary>
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
        => _data.Posts.TryGetValue(postId, out var mapping)
            && ulong.TryParse(mapping.ThreadId, out var id) ? id : null;

    /// <summary>
    /// Retourne les IDs de tous les messages Discord du post (dans l'ordre),
    /// ou une liste vide si le post n'a jamais été publié.
    /// </summary>
    public IReadOnlyList<ulong> GetMessageIds(string postId)
    {
        if (!_data.Posts.TryGetValue(postId, out var mapping))
            return [];

        return mapping.MessageIds
            .Select(raw => ulong.TryParse(raw, out var id) ? id : (ulong?)null)
            .OfType<ulong>()
            .ToList();
    }

    /// <summary>Enregistre le mapping complet d'un post (thread + tous les messages).</summary>
    public void SetPostMapping(string postId, ulong threadId, IEnumerable<ulong> messageIds)
    {
        _data.Posts[postId] = new PostMapping
        {
            ThreadId = threadId.ToString(),
            MessageIds = messageIds.Select(id => id.ToString()).ToList(),
        };
    }

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

    // ─── Modèles internes ────────────────────────────────────────────────────

    private sealed class MappingData
    {
        [JsonPropertyName("sections")]
        public Dictionary<string, string> Sections { get; set; } = [];

        [JsonPropertyName("posts")]
        public Dictionary<string, PostMapping> Posts { get; set; } = [];
    }

    private sealed class PostMapping
    {
        [JsonPropertyName("threadId")]
        public string ThreadId { get; set; } = string.Empty;

        [JsonPropertyName("messageIds")]
        public List<string> MessageIds { get; set; } = [];
    }
}
