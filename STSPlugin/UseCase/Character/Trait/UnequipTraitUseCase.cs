using STSPlugin.Domain;
using STSPlugin.Repository;

namespace STSPlugin.UseCases;

/// <summary>
/// Cas d'usage : retirer un trait équipé d'un personnage.
/// </summary>
public interface UnequipTraitUseCase
{
    /// <summary>
    /// Retire le trait du personnage et persiste la modification.
    /// Si le trait n'est pas équipé, l'opération est ignorée silencieusement.
    /// </summary>
    /// <param name="character">Le personnage cible.</param>
    /// <param name="traitId">L'identifiant du trait à retirer.</param>
    void Execute(Character character, TraitId traitId);
}

/// <summary>
/// Implémentation par défaut de <see cref="UnequipTraitUseCase"/>.
/// </summary>
public class DefaultUnequipTraitUseCase : UnequipTraitUseCase
{
    private readonly CharacterRepository _repository;

    public DefaultUnequipTraitUseCase(CharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public void Execute(Character character, TraitId traitId)
    {
        if (!character.EquippedTraits.Remove(traitId)) return;
        _repository.Save(character);
    }
}
