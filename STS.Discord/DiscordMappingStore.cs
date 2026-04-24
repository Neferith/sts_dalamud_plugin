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
///       "messages": [
///         { "id": "9876543210987654321", "imageUrl": null },
///         { "id": "1111111111111111111", "imageUrl": "https://api.nlrp.fr/images/xxx.png" }
///       ]
///     }
///   }
/// }
/// </code>
/// La clé <c>sections</c> associe un <c>RulesSection.Id</c> à l'ID du Forum Channel Discord.
/// La clé <c>posts</c> associe un <c>RulesPost.Id</c> à son mapping Discord complet.
/// Le premier message est toujours le message d'ouverture du thread (même ID que le thread).
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
    /// Désérialise le fichier en gérant la migration des anciens formats :
    /// - v1 : posts : string (juste un threadId)
    /// - v2 : posts : { threadId, messageIds: string[] }
    /// - v3 : posts : { threadId, messages: [{ id, imageUrl }] }  ← format actuel
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

        // ── Posts (avec migration des anciens formats) ────────────────────────
        if (root.TryGetProperty("posts", out var postsEl))
        {
            foreach (var prop in postsEl.EnumerateObject())
            {
                data.Posts[prop.Name] = prop.Value.ValueKind switch
                {
                    // v1 : "postId": "threadId"
                    JsonValueKind.String => MigrateFromV1(prop.Value.GetString() ?? string.Empty),

                    JsonValueKind.Object => MigrateFromObject(prop.Value),

                    _ => new PostMapping(),
                };
            }
        }

        return data;
    }

    private static PostMapping MigrateFromV1(string threadId) => new()
    {
        ThreadId = threadId,
        Messages = [new MessageEntry { Id = threadId }],
    };

    private static PostMapping MigrateFromObject(JsonElement obj)
    {
        var threadId = obj.TryGetProperty("threadId", out var tid)
            ? tid.GetString() ?? string.Empty
            : string.Empty;

        // v3 : { threadId, messages: [...] }
        if (obj.TryGetProperty("messages", out var messagesEl))
        {
            return new PostMapping
            {
                ThreadId = threadId,
                Messages = messagesEl.Deserialize<List<MessageEntry>>(JsonOptions) ?? [],
            };
        }

        // v2 : { threadId, messageIds: string[] }
        if (obj.TryGetProperty("messageIds", out var idsEl))
        {
            return new PostMapping
            {
                ThreadId = threadId,
                Messages = idsEl.EnumerateArray()
                    .Select(e => new MessageEntry { Id = e.GetString() ?? string.Empty })
                    .ToList(),
            };
        }

        return new PostMapping { ThreadId = threadId };
    }

    /// <summary>Retourne tous les mappings post → threadId (postId → threadId string).</summary>
    public IReadOnlyDictionary<string, string> GetAllPostMappings()
        => _data.Posts.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ThreadId);

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
    /// Retourne les entrées de messages Discord du post (dans l'ordre),
    /// ou une liste vide si le post n'a jamais été publié.
    /// </summary>
    public IReadOnlyList<MessageEntry> GetMessages(string postId)
        => _data.Posts.TryGetValue(postId, out var mapping)
            ? mapping.Messages
            : [];

    /// <summary>Enregistre le mapping complet d'un post (thread + messages avec images).</summary>
    public void SetPostMapping(string postId, ulong threadId, IEnumerable<MessageEntry> messages)
    {
        _data.Posts[postId] = new PostMapping
        {
            ThreadId = threadId.ToString(),
            Messages = messages.ToList(),
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

        [JsonPropertyName("messages")]
        public List<MessageEntry> Messages { get; set; } = [];
    }
}

// ─── Modèle public ───────────────────────────────────────────────────────────

/// <summary>Entrée de message Discord avec l'URL de l'image associée.</summary>
public sealed class MessageEntry
{
    /// <summary>ID du message Discord.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>URL de l'image postée en attachment, ou <see langword="null"/> si aucune.</summary>
    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }

    /// <summary>Retourne l'ID parsé en ulong, ou <see langword="null"/> si invalide.</summary>
    [JsonIgnore]
    public ulong? DiscordId => ulong.TryParse(Id, out var id) ? id : null;
}
