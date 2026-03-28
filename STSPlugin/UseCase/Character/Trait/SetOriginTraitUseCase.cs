using STSPlugin.Domain;
using STSPlugin.Repository;

namespace STSPlugin.UseCases;

/// <summary>
/// Cas d'usage : définir le trait d'origine d'un personnage.
/// Le trait d'origine est gratuit, hors quota, et unique — un seul à la fois.
/// La vérification de la certification associée est laissée au MJ.
/// </summary>
public interface SetOriginTraitUseCase
{
    /// <summary>
    /// Assigne un trait d'origine au personnage et persiste la modification.
    /// Passer null désélectionne le trait d'origine actuel.
    /// </summary>
    /// <param name="character">Le personnage à modifier.</param>
    /// <param name="traitId">L'identifiant du trait d'origine, ou null pour le retirer.</param>
    void Execute(Character character, TraitId? traitId);
}

/// <summary>
/// Implémentation par défaut de <see cref="SetOriginTraitUseCase"/>.
/// </summary>
public class DefaultSetOriginTraitUseCase : SetOriginTraitUseCase
{
    private readonly CharacterRepository _repository;

    public DefaultSetOriginTraitUseCase(CharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public void Execute(Character character, TraitId? traitId)
    {
        // Vérifier que le trait appartient bien à la catégorie Origine
        if (traitId is { } id && Trait.Get(id).Category != TraitCategory.Origine)
            return;

        character.OriginTrait = traitId;
        _repository.Save(character);
    }
}
