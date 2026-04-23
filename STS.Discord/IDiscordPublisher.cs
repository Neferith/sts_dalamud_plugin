using Sts.Domain.Content;

namespace Sts.Discord;

/// <summary>
/// Publie, met à jour et supprime les posts de règles sur Discord.
/// </summary>
public interface IDiscordPublisher
{
    /// <summary>
    /// Crée un thread dans le Forum Discord associé à <paramref name="sectionId"/>
    /// et y poste le contenu de <paramref name="post"/>.
    /// </summary>
    /// <param name="post">Post à publier.</param>
    /// <param name="sectionId">Identifiant de la section parente (détermine le Forum cible).</param>
    /// <param name="ct">Jeton d'annulation.</param>
    Task PublishPostAsync(RulesPost post, string sectionId, CancellationToken ct = default);

    /// <summary>
    /// Édite le message d'ouverture du thread Discord correspondant à <paramref name="post"/>.
    /// Sans effet si le post n'a jamais été publié.
    /// </summary>
    /// <param name="post">Post mis à jour.</param>
    /// <param name="ct">Jeton d'annulation.</param>
    Task UpdatePostAsync(RulesPost post, CancellationToken ct = default);

    /// <summary>
    /// Archive le thread Discord correspondant à <paramref name="post"/>.
    /// Sans effet si le post n'a jamais été publié.
    /// </summary>
    /// <param name="post">Post à supprimer.</param>
    /// <param name="ct">Jeton d'annulation.</param>
    Task DeletePostAsync(RulesPost post, CancellationToken ct = default);
}
