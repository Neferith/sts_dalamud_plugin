using System.Threading.Tasks;
using Sts.Domain.Character;

namespace STSPlugin.CharacterUseCases;

/// <summary>Cas d'usage : retirer un trait équipé d'un personnage.</summary>
public interface UnequipTraitUseCase
{
    /// <summary>
    /// Retire le trait du personnage et persiste la modification.
    /// Si le trait n'est pas équipé, l'opération est ignorée silencieusement.
    /// </summary>
    /// <param name="character">Le personnage cible.</param>
    /// <param name="traitId">L'identifiant du trait à retirer.</param>
    Task ExecuteAsync(Character character, string traitId);
}

/// <summary>Implémentation par défaut de <see cref="UnequipTraitUseCase"/>.</summary>
public class DefaultUnequipTraitUseCase : UnequipTraitUseCase
{
    private readonly ICharacterRepository _characterRepository;

    public DefaultUnequipTraitUseCase(ICharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    /// <inheritdoc/>
    public async Task ExecuteAsync(Character character, string traitId)
    {
        if (!character.EquippedTraitIds.Remove(traitId)) return;
        await _characterRepository.SaveAsync(character);
    }
}
