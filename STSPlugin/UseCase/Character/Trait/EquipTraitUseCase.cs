using STSPlugin.Domain;
using STSPlugin.Repository;
using System.Linq;

namespace STSPlugin.UseCases;

/// <summary>
/// Résultat de la tentative d'équipement d'un trait.
/// </summary>
public enum EquipTraitResult
{
    /// <summary>Le trait a été équipé avec succès.</summary>
    Success,

    /// <summary>Le trait est déjà équipé.</summary>
    AlreadyEquipped,

    /// <summary>Plus de slots de traits disponibles.</summary>
    NoSlotAvailable,

    /// <summary>Le job requis pour ce trait n'est pas celui du personnage.</summary>
    JobMismatch,

    /// <summary>Un trait du même groupe d'exclusivité est déjà équipé.</summary>
    ExclusivityConflict,
}

/// <summary>
/// Cas d'usage : équiper un trait sur un personnage.
/// Valide toutes les règles d'équipement avant de persister.
/// </summary>
public interface EquipTraitUseCase
{
    /// <summary>
    /// Tente d'équiper le trait sur le personnage.
    /// </summary>
    /// <param name="character">Le personnage cible.</param>
    /// <param name="traitId">L'identifiant du trait à équiper.</param>
    /// <returns>Le résultat de la tentative.</returns>
    EquipTraitResult Execute(Character character, TraitId traitId);
}

/// <summary>
/// Implémentation par défaut de <see cref="EquipTraitUseCase"/>.
/// </summary>
public class DefaultEquipTraitUseCase : EquipTraitUseCase
{
    private readonly CharacterRepository _repository;

    public DefaultEquipTraitUseCase(CharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public EquipTraitResult Execute(Character character, TraitId traitId)
    {
        if (character.HasTrait(traitId))
            return EquipTraitResult.AlreadyEquipped;

        if (character.FreeTraitSlots <= 0)
            return EquipTraitResult.NoSlotAvailable;

        var trait = Trait.Get(traitId);

        if (trait.RequiredJob != null && trait.RequiredJob != character.Job)
            return EquipTraitResult.JobMismatch;

        if (trait.ExclusiveGroup != null)
        {
            var conflict = character.EquippedTraits.Any(e =>
                Trait.Get(e).ExclusiveGroup == trait.ExclusiveGroup);
            if (conflict) return EquipTraitResult.ExclusivityConflict;
        }

        character.EquippedTraits.Add(traitId);
        _repository.Save(character);
        return EquipTraitResult.Success;
    }
}
