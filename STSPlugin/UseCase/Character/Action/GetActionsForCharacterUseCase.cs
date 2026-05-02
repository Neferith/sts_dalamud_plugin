using System.Collections.Generic;
using System.Linq;
using Sts.Domain;
using Sts.Domain.Repository;
using Sts.Domain.Character;

namespace STSPlugin.CharacterUseCases;

/// <summary>
/// Cas d'usage : récupérer les actions affichées dans la quickbar pour un personnage.
/// Si QuickbarActionIds est vide, retourne toutes les actions disponibles.
/// Sinon, retourne uniquement les actions sélectionnées par le joueur.
/// </summary>
public interface GetActionsForCharacterUseCase
{
    /// <summary>
    /// Retourne les actions à afficher dans la quickbar pour ce personnage.
    /// </summary>
    IReadOnlyList<RollAction> Execute(Character character);

    /// <summary>
    /// Retourne toutes les actions disponibles (prédéfinies + custom),
    /// quelle que soit la sélection quickbar.
    /// </summary>
    IReadOnlyList<RollAction> GetAll(Character character);
}

/// <summary>
/// Implémentation par défaut de <see cref="GetActionsForCharacterUseCase"/>.
/// </summary>
public class DefaultGetActionsForCharacterUseCase : GetActionsForCharacterUseCase
{
    private readonly ActionRepository _actionRepository;

    public DefaultGetActionsForCharacterUseCase(ActionRepository actionRepository)
        => _actionRepository = actionRepository;

    /// <inheritdoc/>
    public IReadOnlyList<RollAction> Execute(Character character)
    {
        var all = GetAll(character);

        if (character.QuickbarActionIds.Count == 0)
            return all;

        return [.. all.Where(a => character.QuickbarActionIds.Contains(a.Id))];
    }

    /// <inheritdoc/>
    public IReadOnlyList<RollAction> GetAll(Character character)
        => [.. _actionRepository.GetAll(), .. character.CustomActions];
}
