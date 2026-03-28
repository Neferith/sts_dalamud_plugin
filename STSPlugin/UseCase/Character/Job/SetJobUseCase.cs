using STSPlugin.Domain;
using STSPlugin.Repository;

namespace STSPlugin.UseCases;

/// <summary>
/// Cas d'usage : définir le job d'un personnage.
/// Attention : changer de job ne retire pas les traits de job déjà équipés —
/// c'est à l'UI de proposer le nettoyage si nécessaire.
/// </summary>
public interface SetJobUseCase
{
    /// <summary>
    /// Assigne un job au personnage et persiste la modification.
    /// </summary>
    /// <param name="character">Le personnage à modifier.</param>
    /// <param name="job">Le job à assigner.</param>
    void Execute(Character character, Job job);
}

/// <summary>
/// Implémentation par défaut de <see cref="SetJobUseCase"/>.
/// </summary>
public class DefaultSetJobUseCase : SetJobUseCase
{
    private readonly CharacterRepository _repository;

    public DefaultSetJobUseCase(CharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public void Execute(Character character, Job job)
    {
        character.Job = job;
        _repository.Save(character);
    }
}
