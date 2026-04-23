using Microsoft.Extensions.Logging;
using Sts.Domain.Content;
using Sts.Domain.Content.UseCases;

namespace Sts.Discord.Decorators;

/// <summary>
/// Décorateur de <see cref="IDeletePostUseCase"/> qui archive le thread Discord
/// après une suppression réussie.
/// </summary>
public sealed class DiscordAwareDeletePostUseCase : IDeletePostUseCase
{
    private readonly IDeletePostUseCase _inner;
    private readonly IDiscordPublisher _publisher;
    private readonly ILogger<DiscordAwareDeletePostUseCase> _logger;

    /// <param name="inner">Use case de suppression original.</param>
    /// <param name="publisher">Publicitaire Discord.</param>
    /// <param name="logger">Logger.</param>
    public DiscordAwareDeletePostUseCase(
        IDeletePostUseCase inner,
        IDiscordPublisher publisher,
        ILogger<DiscordAwareDeletePostUseCase> logger)
    {
        _inner = inner;
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> ExecuteAsync(string sectionId, string postId)
    {
        var result = await _inner.ExecuteAsync(sectionId, postId);

        if (result)
        {
            try
            {
                var post = new RulesPost { Id = postId };
                await _publisher.DeletePostAsync(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Échec de l'archivage Discord pour le post '{PostId}' (section '{SectionId}').",
                    postId, sectionId);
            }
        }

        return result;
    }
}
