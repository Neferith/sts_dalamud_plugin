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

    private readonly DiscordSocketClient _client;
    private readonly DiscordMappingStore _mappingStore;
    private readonly string _botToken;
    private readonly ILogger<DiscordBotService> _logger;

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

        var chunks = SplitContent(post.Content);
        var messageIds = new List<ulong>();

        // Le premier chunk devient le message d'ouverture du thread.
        var thread = await forum.CreatePostAsync(
            title: post.Title,
            archiveDuration: ThreadArchiveDuration.OneWeek,
            text: chunks[0]);

        // Dans un Forum Discord, l'ID du thread == l'ID du message d'ouverture.
        messageIds.Add(thread.Id);

        // Les chunks suivants sont postés en replies dans le thread.
        for (var i = 1; i < chunks.Count; i++)
        {
            var reply = await thread.SendMessageAsync(chunks[i]);
            messageIds.Add(reply.Id);
        }

        _mappingStore.SetPostMapping(post.Id, thread.Id, messageIds);
        await _mappingStore.SaveAsync(ct);

        _logger.LogInformation(
            "Post '{PostId}' publié sur Discord (thread {ThreadId}, {Count} message(s)).",
            post.Id, thread.Id, messageIds.Count);
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

        var oldMessageIds = _mappingStore.GetMessageIds(post.Id);
        var newChunks = SplitContent(post.Content);
        var newMessageIds = new List<ulong>();

        // ── Éditer ou créer les messages ─────────────────────────────────────

        for (var i = 0; i < newChunks.Count; i++)
        {
            if (i < oldMessageIds.Count)
            {
                // Message existant → édition.
                var existingMsg = await thread.GetMessageAsync(oldMessageIds[i]);
                if (existingMsg is IUserMessage userMsg)
                {
                    await userMsg.ModifyAsync(m => m.Content = newChunks[i]);
                    newMessageIds.Add(oldMessageIds[i]);
                }
                else
                {
                    _logger.LogWarning(
                        "Message {MessageId} introuvable ou non éditable pour le post '{PostId}', remplacement.",
                        oldMessageIds[i], post.Id);
                    var replacement = await thread.SendMessageAsync(newChunks[i]);
                    newMessageIds.Add(replacement.Id);
                }
            }
            else
            {
                // Nouveau chunk → nouveau message.
                var newMsg = await thread.SendMessageAsync(newChunks[i]);
                newMessageIds.Add(newMsg.Id);
            }
        }

        // ── Supprimer les messages en surplus ────────────────────────────────

        for (var i = newChunks.Count; i < oldMessageIds.Count; i++)
        {
            var surplusMsg = await thread.GetMessageAsync(oldMessageIds[i]);
            if (surplusMsg is not null)
                await surplusMsg.DeleteAsync();
        }

        _mappingStore.SetPostMapping(post.Id, threadId.Value, newMessageIds);
        await _mappingStore.SaveAsync(ct);

        _logger.LogInformation(
            "Post '{PostId}' mis à jour sur Discord (thread {ThreadId}, {Count} message(s)).",
            post.Id, threadId.Value, newMessageIds.Count);
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
            // On archive plutôt que supprimer pour préserver l'historique.
            await thread.ModifyAsync(t => t.Archived = true);

            _logger.LogInformation(
                "Thread {ThreadId} archivé sur Discord (post '{PostId}' supprimé).",
                threadId.Value, post.Id);
        }

        _mappingStore.RemovePost(post.Id);
        await _mappingStore.SaveAsync(ct);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Attend que le client Discord soit prêt.</summary>
    private Task WaitReadyAsync(CancellationToken ct)
        => _readyTcs.Task.WaitAsync(ct);

    /// <summary>
    /// Découpe le contenu d'un post sur le marqueur <c>---split---</c>.
    /// Retourne au moins un chunk non vide.
    /// </summary>
    private static List<string> SplitContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [string.Empty];

        var chunks = content
            .Split(SplitMarker, StringSplitOptions.None)
            .Select(c => c.Trim('\n', '\r'))
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        return chunks.Count > 0 ? chunks : [string.Empty];
    }

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
