using Sts.Domain.Content;

namespace Sts.Discord;

/// <summary>
/// Implémentation no-op de <see cref="IDiscordPublisher"/>.
/// Utilisée quand aucun token Discord n'est configuré.
/// </summary>
public sealed class NullDiscordPublisher : IDiscordPublisher
{
    /// <inheritdoc/>
    public Task PublishPostAsync(RulesPost post, string sectionId, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task UpdatePostAsync(RulesPost post, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task DeletePostAsync(RulesPost post, CancellationToken ct = default)
        => Task.CompletedTask;
}
