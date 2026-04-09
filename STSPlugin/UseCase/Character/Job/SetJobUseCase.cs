using Sts.Domain;
using STSPlugin.Repository;

namespace STSPlugin.CharacterUseCases;

/// <summary>
/// Cas d'usage : définir le job d'un personnage.
/// Vérifie que le job existe dans le repository avant de l'assigner.
/// </summary>
public interface SetJobUseCase
{
    /// <summary>
    /// Assigne un job au personnage et persiste la modification.
    /// Passer null retire le job actuel.
    /// Si l'id ne correspond à aucun job connu, l'opération est ignorée.
    /// </summary>
    /// <param name="character">Le personnage à modifier.</param>
    /// <param name="jobId">L'identifiant du job, ou null pour retirer le job.</param>
    void Execute(Character character, string? jobId);
}

/// <summary>
/// Implémentation par défaut de <see cref="SetJobUseCase"/>.
/// </summary>
public class DefaultSetJobUseCase : SetJobUseCase
{
    private readonly CharacterRepository _characterRepository;
    private readonly JobRepository _jobRepository;

    public DefaultSetJobUseCase(CharacterRepository characterRepository, JobRepository jobRepository)
    {
        _characterRepository = characterRepository;
        _jobRepository = jobRepository;
    }

    /// <inheritdoc/>
    public void Execute(Character character, string? jobId)
    {
        if (jobId != null && _jobRepository.GetById(jobId) is null)
            return;

        character.JobId = jobId;
        _characterRepository.Save(character);
    }
}
