using System;
using System.Collections.Generic;
using System.Linq;
using STSPlugin.Domain;
using STSPlugin.Repository;

namespace STSPlugin.UseCases;

/// <summary>
/// Cas d'usage : créer une action personnalisée pour un personnage.
/// </summary>
public interface CreateCustomActionUseCase
{
    /// <summary>
    /// Crée une action personnalisée et la persiste dans la fiche du personnage.
    /// </summary>
    /// <param name="character">Le personnage cible.</param>
    /// <param name="name">Nom de l'action.</param>
    /// <param name="contexts">Contextes du jet.</param>
    /// <returns>L'action créée.</returns>
    RollAction Execute(Character character, string name, IReadOnlyList<string> contexts);
}

/// <summary>
/// Implémentation par défaut de <see cref="CreateCustomActionUseCase"/>.
/// </summary>
public class DefaultCreateCustomActionUseCase : CreateCustomActionUseCase
{
    private readonly CharacterRepository _characterRepository;

    public DefaultCreateCustomActionUseCase(CharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    /// <inheritdoc/>
    public RollAction Execute(Character character, string name, IReadOnlyList<string> contexts)
    {
        var action = new RollAction
        {
            Id = Guid.NewGuid().ToString(),
            Name = name.Trim(),
            Contexts = [.. contexts],
            IsPredefined = false,
        };

        character.CustomActions.Add(action);
        _characterRepository.Save(character);
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
    void Execute(Character character, string actionId);
}

/// <summary>
/// Implémentation par défaut de <see cref="DeleteCustomActionUseCase"/>.
/// </summary>
public class DefaultDeleteCustomActionUseCase : DeleteCustomActionUseCase
{
    private readonly CharacterRepository _characterRepository;

    public DefaultDeleteCustomActionUseCase(CharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    /// <inheritdoc/>
    public void Execute(Character character, string actionId)
    {
        var action = character.CustomActions.FirstOrDefault(a => a.Id == actionId);
        if (action is null || action.IsPredefined) return;

        character.CustomActions.Remove(action);
        _characterRepository.Save(character);
    }
}
