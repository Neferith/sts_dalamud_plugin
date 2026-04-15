using Microsoft.Extensions.Logging;
using Sts.Domain.Content;
using Sts.Domain.Content.UseCases;

namespace Sts.Discord.Decorators;

/// <summary>
/// Décorateur de <see cref="ICreatePostUseCase"/> qui publie le post sur Discord
/// après une création réussie.
/// </summary>
public sealed class DiscordAwareCreatePostUseCase : ICreatePostUseCase
{
    private readonly ICreatePostUseCase _inner;
    private readonly IDiscordPublisher _publisher;
    private readonly ILogger<DiscordAwareCreatePostUseCase> _logger;

    /// <param name="inner">Use case de création original.</param>
    /// <param name="publisher">Publicitaire Discord.</param>
    /// <param name="logger">Logger.</param>
    public DiscordAwareCreatePostUseCase(
        ICreatePostUseCase inner,
        IDiscordPublisher publisher,
        ILogger<DiscordAwareCreatePostUseCase> logger)
    {
        _inner = inner;
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool?> ExecuteAsync(string sectionId, RulesPost post)
    {
        var result = await _inner.ExecuteAsync(sectionId, post);

        if (result is true)
        {
            try
            {
                await _publisher.PublishPostAsync(post, sectionId);
            }
            catch (Exception ex)
            {
                // Une erreur Discord ne doit pas faire échouer la création STS.
                _logger.LogError(ex,
                    "Échec de la publication Discord pour le post '{PostId}' (section '{SectionId}').",
                    post.Id, sectionId);
            }
        }

        return result;
    }
}
