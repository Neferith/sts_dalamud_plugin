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
    private readonly DiscordSocketClient _client;
    private readonly DiscordMappingStore _mappingStore;
    private readonly string _botToken;
    private readonly ILogger<DiscordBotService> _logger;

    /// <summary>
    /// Complété quand le client Discord est prêt à accepter des appels.
    /// </summary>
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

        // Maintenir le service en vie jusqu'à l'arrêt de l'application.
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

        var thread = await forum.CreatePostAsync(
            title: post.Title,
            archiveDuration: ThreadArchiveDuration.OneWeek,
            text: FormatContent(post));

        // Dans un Forum Discord, l'ID du thread == l'ID du message d'ouverture.
        _mappingStore.SetThreadId(post.Id, thread.Id);
        await _mappingStore.SaveAsync(ct);

        _logger.LogInformation(
            "Post '{PostId}' publié sur Discord (thread {ThreadId}).",
            post.Id, thread.Id);
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

        // Le message d'ouverture d'un thread Forum a le même ID que le thread.
        var message = await thread.GetMessageAsync(threadId.Value);
        if (message is IUserMessage userMessage)
        {
            await userMessage.ModifyAsync(m => m.Content = FormatContent(post));

            _logger.LogInformation(
                "Post '{PostId}' mis à jour sur Discord (thread {ThreadId}).",
                post.Id, threadId.Value);
        }
        else
        {
            _logger.LogError(
                "Message d'ouverture {MessageId} introuvable ou non éditable pour le post '{PostId}'.",
                threadId.Value, post.Id);
        }
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
    /// Formate le contenu d'un post pour Discord.
    /// Le contenu STS est déjà en Markdown — Discord l'accepte nativement.
    /// </summary>
    private static string FormatContent(RulesPost post)
        => post.Content;

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
