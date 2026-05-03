using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sts.Domain;
using Sts.Domain.Character;

namespace STSPlugin.CharacterUseCases;

/// <summary>Cas d'usage : créer une action personnalisée pour un personnage.</summary>
public interface CreateCustomActionUseCase
{
    /// <summary>
    /// Crée une action personnalisée et la persiste dans la fiche du personnage.
    /// </summary>
    /// <param name="character">Le personnage cible.</param>
    /// <param name="name">Nom de l'action.</param>
    /// <param name="contexts">Contextes du jet.</param>
    /// <returns>L'action créée.</returns>
    Task<RollAction> ExecuteAsync(Character character, string name, IReadOnlyList<string> contexts);
}

/// <summary>Implémentation par défaut de <see cref="CreateCustomActionUseCase"/>.</summary>
public class DefaultCreateCustomActionUseCase : CreateCustomActionUseCase
{
    private readonly ICharacterRepository _characterRepository;

    public DefaultCreateCustomActionUseCase(ICharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    /// <inheritdoc/>
    public async Task<RollAction> ExecuteAsync(Character character, string name, IReadOnlyList<string> contexts)
    {
        var action = new RollAction
        {
            Id           = Guid.NewGuid().ToString(),
            Name         = name.Trim(),
            Contexts     = [.. contexts],
            IsPredefined = false,
        };

        character.CustomActions.Add(action);
        await _characterRepository.SaveAsync(character);
        return action;
    }
}

/// <summary>
/// Cas d'usage : supprimer une action personnalisée d'un personnage.
/// Les actions prédéfinies ne peuvent pas être supprimées.
/// </summary>
public interface DeleteCustomActionUseCase
{
    /// <summary>
    /// Supprime l'action personnalisée du personnage et persiste la modification.
    /// Si l'action est prédéfinie ou introuvable, l'opération est ignorée.
    /// </summary>
    /// <param name="character">Le personnage cible.</param>
    /// <param name="actionId">L'identifiant de l'action à supprimer.</param>
    Task ExecuteAsync(Character character, string actionId);
}

/// <summary>Implémentation par défaut de <see cref="DeleteCustomActionUseCase"/>.</summary>
public class DefaultDeleteCustomActionUseCase : DeleteCustomActionUseCase
{
    private readonly ICharacterRepository _characterRepository;

    public DefaultDeleteCustomActionUseCase(ICharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    /// <inheritdoc/>
    public async Task ExecuteAsync(Character character, string actionId)
    {
        var action = character.CustomActions.FirstOrDefault(a => a.Id == actionId);
        if (action is null || action.IsPredefined) return;

        character.CustomActions.Remove(action);
        await _characterRepository.SaveAsync(character);
    }
}
