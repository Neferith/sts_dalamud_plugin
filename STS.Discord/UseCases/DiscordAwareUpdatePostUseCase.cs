using Microsoft.Extensions.Logging;
using Sts.Domain.Content;
using Sts.Domain.Content.UseCases;

namespace Sts.Discord.Decorators;

/// <summary>
/// Décorateur de <see cref="IUpdatePostUseCase"/> qui met à jour le thread Discord
/// après une modification réussie.
/// </summary>
public sealed class DiscordAwareUpdatePostUseCase : IUpdatePostUseCase
{
    private readonly IUpdatePostUseCase _inner;
    private readonly IDiscordPublisher _publisher;
    private readonly ILogger<DiscordAwareUpdatePostUseCase> _logger;

    /// <param name="inner">Use case de mise à jour original.</param>
    /// <param name="publisher">Publicitaire Discord.</param>
    /// <param name="logger">Logger.</param>
    public DiscordAwareUpdatePostUseCase(
        IUpdatePostUseCase inner,
        IDiscordPublisher publisher,
        ILogger<DiscordAwareUpdatePostUseCase> logger)
    {
        _inner = inner;
        _publisher = publisher;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<bool> ExecuteAsync(string sectionId, string postId, string title, string content)
    {
        var result = await _inner.ExecuteAsync(sectionId, postId, title, content);

        if (result)
        {
            try
            {
                // On reconstruit le post depuis les paramètres — suffisant pour Discord.
                var post = new RulesPost { Id = postId, Title = title, Content = content };
                await _publisher.UpdatePostAsync(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Échec de la mise à jour Discord pour le post '{PostId}' (section '{SectionId}').",
                    postId, sectionId);
            }
        }

        return result;
    }
}
