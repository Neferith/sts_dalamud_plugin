using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sts.Domain.Content;

namespace Sts.Discord;

/// <summary>
/// Service hébergé qui maintient la connexion au bot Discord
/// et implémente <see cref="IDiscordPublisher"/>.
/// </summary>
public sealed class DiscordBotService : BackgroundService, IDiscordPublisher
{
    private const string SplitMarker = "---split---";
    private const string ImageMarker = "---image---";

    private readonly DiscordSocketClient _client;
    private readonly DiscordMappingStore _mappingStore;
    private readonly string _botToken;
    private readonly ILogger<DiscordBotService> _logger;
    private readonly HttpClient _http = new();

    /// <summary>Complété quand le client Discord est prêt à accepter des appels.</summary>
    private readonly TaskCompletionSource _readyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <param name="mappingStore">Store de mappings STS ↔ Discord.</param>
    /// <param name="botToken">Token du bot Discord.</param>
    /// <param name="logger">Logger.</param>
    public DiscordBotService(
        DiscordMappingStore mappingStore,
        string botToken,
        ILogger<DiscordBotService> logger)
    {
        _mappingStore = mappingStore;
        _botToken = botToken;
        _logger = logger;

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds,
            LogLevel = LogSeverity.Warning,
        });

        _client.Log += OnLog;
        _client.Ready += OnReady;
    }

    // ─── BackgroundService ───────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _mappingStore.LoadAsync(stoppingToken);

        await _client.LoginAsync(TokenType.Bot, _botToken);
        await _client.StartAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Arrêt normal.
        }
        finally
        {
            await _client.StopAsync();
        }
    }

    // ─── IDiscordPublisher ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task PublishPostAsync(RulesPost post, string sectionId, CancellationToken ct = default)
    {
        await WaitReadyAsync(ct);

        var channelId = _mappingStore.GetForumChannelId(sectionId);
        if (channelId is null)
        {
            _logger.LogWarning(
                "Aucun Forum Channel Discord configuré pour la section '{SectionId}'. Publication ignorée.",
                sectionId);
            return;
        }

        if (_client.GetChannel(channelId.Value) is not IForumChannel forum)
        {
            _logger.LogError(
                "Le channel {ChannelId} est introuvable ou n'est pas un Forum Channel.",
                channelId.Value);
            return;
        }

        var blocks = ParseBlocks(post.Content);
        var entries = new List<MessageEntry>();

        // Le premier bloc devient le message d'ouverture du thread.
        var thread = await SendBlockAsync(forum, post.Title, blocks[0], ct);
        entries.Add(new MessageEntry { Id = thread.Id.ToString(), ImageUrl = blocks[0].ImageUrl });

        // Les blocs suivants sont postés en replies dans le thread.
        for (var i = 1; i < blocks.Count; i++)
        {
            var msg = await SendBlockAsync(thread, blocks[i], ct);
            entries.Add(new MessageEntry { Id = msg.Id.ToString(), ImageUrl = blocks[i].ImageUrl });
        }

        _mappingStore.SetPostMapping(post.Id, thread.Id, entries);
        await _mappingStore.SaveAsync(ct);

        _logger.LogInformation(
            "Post '{PostId}' publié sur Discord (thread {ThreadId}, {Count} bloc(s)).",
            post.Id, thread.Id, entries.Count);
    }

    /// <inheritdoc/>
    public async Task UpdatePostAsync(RulesPost post, CancellationToken ct = default)
    {
        await WaitReadyAsync(ct);

        var threadId = _mappingStore.GetThreadId(post.Id);
        if (threadId is null)
        {
            _logger.LogDebug(
                "Post '{PostId}' jamais publié sur Discord, mise à jour ignorée.",
                post.Id);
            return;
        }

        if (_client.GetChannel(threadId.Value) is not IThreadChannel thread)
        {
            _logger.LogError(
                "Thread {ThreadId} introuvable pour le post '{PostId}'.",
                threadId.Value, post.Id);
            return;
        }

        var oldMessages = _mappingStore.GetMessages(post.Id);
        var newBlocks = ParseBlocks(post.Content);
        var newEntries = new List<MessageEntry>();

        // ── Éditer ou créer les messages ─────────────────────────────────────

        for (var i = 0; i < newBlocks.Count; i++)
        {
            var block = newBlocks[i];

            if (i < oldMessages.Count)
            {
                var old = oldMessages[i];
                if (old.DiscordId is not { } msgId) continue;

                var existingMsg = await thread.GetMessageAsync(msgId);
                if (existingMsg is IUserMessage userMsg)
                {
                    var imageChanged = old.ImageUrl != block.ImageUrl;

                    if (imageChanged && block.ImageUrl is not null)
                    {
                        // Image changée → supprimer l'ancien message et en créer un nouveau
                        // (Discord ne permet pas de remplacer un attachment par édition).
                        await userMsg.DeleteAsync();
                        var replacement = await SendBlockAsync(thread, block, ct);
                        newEntries.Add(new MessageEntry { Id = replacement.Id.ToString(), ImageUrl = block.ImageUrl });
                    }
                    else if (imageChanged && block.ImageUrl is null)
                    {
                        // Image supprimée → éditer le texte, l'attachment reste (limitation Discord).
                        // On supprime et recrée proprement.
                        await userMsg.DeleteAsync();
                        var replacement = await SendBlockAsync(thread, block, ct);
                        newEntries.Add(new MessageEntry { Id = replacement.Id.ToString(), ImageUrl = null });
                    }
                    else
                    {
                        // Texte seul modifié, pas d'image ou image inchangée.
                        await userMsg.ModifyAsync(m => m.Content = block.Text);
                        newEntries.Add(new MessageEntry { Id = msgId.ToString(), ImageUrl = block.ImageUrl });
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Message {MessageId} introuvable pour le post '{PostId}', recréation.",
                        msgId, post.Id);
                    var replacement = await SendBlockAsync(thread, block, ct);
                    newEntries.Add(new MessageEntry { Id = replacement.Id.ToString(), ImageUrl = block.ImageUrl });
                }
            }
            else
            {
                // Nouveau bloc → nouveau message.
                var newMsg = await SendBlockAsync(thread, block, ct);
                newEntries.Add(new MessageEntry { Id = newMsg.Id.ToString(), ImageUrl = block.ImageUrl });
            }
        }

        // ── Supprimer les messages en surplus ────────────────────────────────

        for (var i = newBlocks.Count; i < oldMessages.Count; i++)
        {
            if (oldMessages[i].DiscordId is not { } surplusId) continue;
            var surplusMsg = await thread.GetMessageAsync(surplusId);
            if (surplusMsg is not null)
                await surplusMsg.DeleteAsync();
        }

        _mappingStore.SetPostMapping(post.Id, threadId.Value, newEntries);
        await _mappingStore.SaveAsync(ct);

        _logger.LogInformation(
            "Post '{PostId}' mis à jour sur Discord (thread {ThreadId}, {Count} bloc(s)).",
            post.Id, threadId.Value, newEntries.Count);
    }

    /// <inheritdoc/>
    public async Task DeletePostAsync(RulesPost post, CancellationToken ct = default)
    {
        await WaitReadyAsync(ct);

        var threadId = _mappingStore.GetThreadId(post.Id);
        if (threadId is null)
        {
            _logger.LogDebug(
                "Post '{PostId}' jamais publié sur Discord, suppression ignorée.",
                post.Id);
            return;
        }

        if (_client.GetChannel(threadId.Value) is IThreadChannel thread)
        {
            await thread.ModifyAsync(t => t.Archived = true);

            _logger.LogInformation(
                "Thread {ThreadId} archivé sur Discord (post '{PostId}' supprimé).",
                threadId.Value, post.Id);
        }

        _mappingStore.RemovePost(post.Id);
        await _mappingStore.SaveAsync(ct);
    }

    // ─── Envoi de blocs ──────────────────────────────────────────────────────

    /// <summary>Crée le thread d'ouverture dans un Forum Channel avec le premier bloc.</summary>
    private async Task<IThreadChannel> SendBlockAsync(
     IForumChannel forum, string title, ContentBlock block, CancellationToken ct)
    {
        var thread = await forum.CreatePostAsync(
            title: title,
            archiveDuration: ThreadArchiveDuration.OneWeek,
            text: block.Text);

        if (block.ImageUrl is not null)
        {
            // Récupérer le thread depuis le cache client plutôt que d'utiliser
            // la référence retournée directement par CreatePostAsync.
            var freshThread = _client.GetChannel(thread.Id) as IThreadChannel ?? thread;
            var (stream, fileName) = await DownloadImageAsync(block.ImageUrl, ct);
            await using (stream)
            {
                await freshThread.SendFileAsync(stream, fileName);
            }
        }

        return thread;
    }

    /// <summary>Poste un bloc dans un thread existant.</summary>
    private async Task<IUserMessage> SendBlockAsync(
        IThreadChannel thread, ContentBlock block, CancellationToken ct)
    {
        if (block.ImageUrl is not null)
        {
            var (stream, fileName) = await DownloadImageAsync(block.ImageUrl, ct);
            await using (stream)
            {
                return await thread.SendFileAsync(stream, fileName, block.Text);
            }
        }

        return await thread.SendMessageAsync(block.Text);
    }

    /// <summary>Télécharge une image depuis une URL et retourne le stream + nom de fichier.</summary>
    private async Task<(Stream stream, string fileName)> DownloadImageAsync(
        string imageUrl, CancellationToken ct)
    {
        var bytes = await _http.GetByteArrayAsync(imageUrl, ct);
        var stream = new MemoryStream(bytes);
        var fileName = Path.GetFileName(new Uri(imageUrl).LocalPath);
        if (string.IsNullOrWhiteSpace(fileName)) fileName = "image.png";
        return (stream, fileName);
    }

    // ─── Parsing ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Découpe le contenu sur <c>---split---</c> puis extrait l'image de chaque bloc
    /// via <c>---image---</c>.
    /// </summary>
    private static List<ContentBlock> ParseBlocks(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [new ContentBlock { Text = string.Empty }];

        var blocks = content
            .Split(SplitMarker, StringSplitOptions.None)
            .Select(ParseBlock)
            .Where(b => !string.IsNullOrWhiteSpace(b.Text) || b.ImageUrl is not null)
            .ToList();

        return blocks.Count > 0 ? blocks : [new ContentBlock { Text = string.Empty }];
    }

    /// <summary>Extrait le texte et l'URL d'image d'un bloc brut.</summary>
    private static ContentBlock ParseBlock(string raw)
    {
        var parts = raw.Split(ImageMarker, 2, StringSplitOptions.None);

        var text = parts[0].Trim('\n', '\r');
        var imageUrl = parts.Length > 1
            ? parts[1].Trim('\n', '\r', ' ')
            : null;

        return new ContentBlock
        {
            Text = text,
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl,
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Attend que le client Discord soit prêt.</summary>
    private Task WaitReadyAsync(CancellationToken ct)
        => _readyTcs.Task.WaitAsync(ct);

    // ─── Événements Discord ──────────────────────────────────────────────────

    private Task OnReady()
    {
        _readyTcs.TrySetResult();
        _logger.LogInformation("Bot Discord connecté et prêt.");
        return Task.CompletedTask;
    }

    private Task OnLog(LogMessage msg)
    {
        var level = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            _ => LogLevel.Debug,
        };

        _logger.Log(level, msg.Exception, "[Discord.Net] {Message}", msg.Message);
        return Task.CompletedTask;
    }
}

// ─── Modèle interne ──────────────────────────────────────────────────────────

/// <summary>Bloc de contenu parsé depuis le Markdown STS.</summary>
internal sealed class ContentBlock
{
    /// <summary>Contenu Markdown du bloc (sans l'image).</summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>URL de l'image associée, ou <see langword="null"/>.</summary>
    public string? ImageUrl { get; init; }
}
