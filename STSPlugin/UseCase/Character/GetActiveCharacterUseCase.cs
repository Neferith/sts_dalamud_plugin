using Sts.Domain.Character;

namespace STSPlugin.CharacterUseCases;

/// <summary>
/// Cas d'usage : récupérer le personnage actuellement actif.
/// Plugin-specific — reste synchrone (appelé depuis le render thread ImGui).
/// </summary>
public interface GetActiveCharacterUseCase
{
    /// <summary>
    /// Retourne le personnage actif, ou null si aucun n'est sélectionné
    /// ou si l'identifiant sauvegardé ne correspond plus à un personnage existant.
    /// </summary>
    Character? Execute();
}

/// <summary>Implémentation par défaut de <see cref="GetActiveCharacterUseCase"/>.</summary>
public class DefaultGetActiveCharacterUseCase : GetActiveCharacterUseCase
{
    private readonly ActiveCharacterState _state;

    public DefaultGetActiveCharacterUseCase(ActiveCharacterState state)
        => _state = state;

    public Character? Execute() => _state.Current;
}
