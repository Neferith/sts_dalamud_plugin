using STSPlugin.Domain;
using STSPlugin.Repository;

namespace STSPlugin.UseCases;

/// <summary>
/// Cas d'usage : mettre à jour les informations d'un personnage existant.
/// </summary>
public interface UpdateCharacterUseCase
{
    /// <summary>
    /// Met à jour le nom et le rang du personnage et persiste les modifications.
    /// Si le personnage n'existe pas, l'opération est ignorée silencieusement.
    /// </summary>
    /// <param name="character">Le personnage avec les nouvelles valeurs.</param>
    void Execute(Character character);
}

/// <summary>
/// Implémentation par défaut de <see cref="UpdateCharacterUseCase"/>.
/// </summary>
public class DefaultUpdateCharacterUseCase : UpdateCharacterUseCase
{
    private readonly CharacterRepository _repository;

    public DefaultUpdateCharacterUseCase(CharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public void Execute(Character character)
        => _repository.Save(character);
}
