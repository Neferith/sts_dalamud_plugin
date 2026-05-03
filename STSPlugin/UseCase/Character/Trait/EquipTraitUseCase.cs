using System.Linq;
using System.Threading.Tasks;
using Sts.Domain;
using Sts.Domain.Character;
using Sts.Domain.Repository;

namespace STSPlugin.CharacterUseCases;

/// <summary>Résultat de la tentative d'équipement d'un trait.</summary>
public enum EquipTraitResult
{
    /// <summary>Le trait a été équipé avec succès.</summary>
    Success,
    /// <summary>Le trait est introuvable dans le repository.</summary>
    TraitNotFound,
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
    Task<EquipTraitResult> ExecuteAsync(Character character, string traitId);
}

/// <summary>Implémentation par défaut de <see cref="EquipTraitUseCase"/>.</summary>
public class DefaultEquipTraitUseCase : EquipTraitUseCase
{
    private readonly ICharacterRepository _characterRepository;
    private readonly TraitRepository      _traitRepository;

    public DefaultEquipTraitUseCase(ICharacterRepository characterRepository, TraitRepository traitRepository)
    {
        _characterRepository = characterRepository;
        _traitRepository     = traitRepository;
    }

    /// <inheritdoc/>
    public async Task<EquipTraitResult> ExecuteAsync(Character character, string traitId)
    {
        var trait = _traitRepository.GetById(traitId);
        if (trait is null)
            return EquipTraitResult.TraitNotFound;

        if (character.HasTrait(traitId))
            return EquipTraitResult.AlreadyEquipped;

        if (character.FreeTraitSlots <= 0)
            return EquipTraitResult.NoSlotAvailable;

        if (trait.RequiredJobIds != null && trait.RequiredJobIds.Count > 0)
        {
            if (character.JobId == null || !trait.RequiredJobIds.Contains(character.JobId))
                return EquipTraitResult.JobMismatch;
        }

        if (trait.ExclusiveGroup != null)
        {
            var conflict = character.EquippedTraitIds
                .Select(id => _traitRepository.GetById(id))
                .Any(t => t?.ExclusiveGroup == trait.ExclusiveGroup);

            if (conflict) return EquipTraitResult.ExclusivityConflict;
        }

        character.EquippedTraitIds.Add(traitId);
        await _characterRepository.SaveAsync(character);
        return EquipTraitResult.Success;
    }
}
