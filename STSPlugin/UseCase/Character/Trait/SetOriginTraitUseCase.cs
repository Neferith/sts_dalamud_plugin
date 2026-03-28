using STSPlugin.Domain;
using STSPlugin.Repository;

namespace STSPlugin.UseCases;

/// <summary>
/// Cas d'usage : définir le trait d'origine d'un personnage.
/// Gratuit, hors quota, un seul à la fois. Doit être de catégorie Origine.
/// </summary>
public interface SetOriginTraitUseCase
{
    /// <summary>
    /// Assigne un trait d'origine au personnage et persiste la modification.
    /// Passer null retire le trait d'origine actuel.
    /// Si l'id ne correspond pas à un trait d'origine valide, l'opération est ignorée.
    /// </summary>
    /// <param name="character">Le personnage à modifier.</param>
    /// <param name="traitId">L'identifiant du trait d'origine, ou null pour le retirer.</param>
    void Execute(Character character, string? traitId);
}

/// <summary>
/// Implémentation par défaut de <see cref="SetOriginTraitUseCase"/>.
/// </summary>
public class DefaultSetOriginTraitUseCase : SetOriginTraitUseCase
{
    private readonly CharacterRepository _characterRepository;
    private readonly TraitRepository _traitRepository;

    public DefaultSetOriginTraitUseCase(CharacterRepository characterRepository, TraitRepository traitRepository)
    {
        _characterRepository = characterRepository;
        _traitRepository = traitRepository;
    }

    /// <inheritdoc/>
    public void Execute(Character character, string? traitId)
    {
        if (traitId != null)
        {
            var trait = _traitRepository.GetById(traitId);
            if (trait is null || trait.Category != TraitCategory.Origine)
                return;
        }

        character.OriginTraitId = traitId;
        _characterRepository.Save(character);
    }
}
