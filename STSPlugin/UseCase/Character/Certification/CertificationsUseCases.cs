using System;
using System.Linq;
using System.Threading.Tasks;
using Sts.Domain;
using Sts.Domain.Character;

namespace STSPlugin.CharacterUseCases;

/// <summary>
/// Cas d'usage : ajouter une certification à un personnage.
/// Seul un officier peut accorder une certification — la validation est MJ.
/// </summary>
public interface AddCertificationUseCase
{
    /// <summary>
    /// Crée et ajoute une certification au personnage.
    /// </summary>
    /// <param name="character">Le personnage cible.</param>
    /// <param name="name">Nom de la certification.</param>
    /// <param name="linkedOriginTraitId">Trait d'origine débloqué (null si aucun).</param>
    /// <param name="linkedAbilityId">Compétence concernée (null si aucune).</param>
    /// <param name="freePoints">Points gratuits accordés sur la compétence liée.</param>
    /// <returns>La certification créée.</returns>
    Task<Certification> ExecuteAsync(
        Character character,
        string name,
        string? linkedOriginTraitId = null,
        string? linkedAbilityId = null,
        int freePoints = 0);
}

/// <summary>Implémentation par défaut de <see cref="AddCertificationUseCase"/>.</summary>
public class DefaultAddCertificationUseCase : AddCertificationUseCase
{
    private readonly ICharacterRepository _characterRepository;

    public DefaultAddCertificationUseCase(ICharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    /// <inheritdoc/>
    public async Task<Certification> ExecuteAsync(
        Character character,
        string name,
        string? linkedOriginTraitId = null,
        string? linkedAbilityId = null,
        int freePoints = 0)
    {
        var certification = new Certification
        {
            Name                = name.Trim(),
            LinkedOriginTraitId = linkedOriginTraitId,
            LinkedAbilityId     = linkedAbilityId,
            FreePoints          = Math.Max(0, freePoints),
        };

        character.Certifications.Add(certification);
        await _characterRepository.SaveAsync(character);
        return certification;
    }
}

/// <summary>Cas d'usage : retirer une certification d'un personnage.</summary>
public interface RemoveCertificationUseCase
{
    /// <summary>
    /// Retire la certification du personnage et persiste la modification.
    /// Si l'id est introuvable, l'opération est ignorée.
    /// </summary>
    Task ExecuteAsync(Character character, string certificationId);
}

/// <summary>Implémentation par défaut de <see cref="RemoveCertificationUseCase"/>.</summary>
public class DefaultRemoveCertificationUseCase : RemoveCertificationUseCase
{
    private readonly ICharacterRepository _characterRepository;

    public DefaultRemoveCertificationUseCase(ICharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    /// <inheritdoc/>
    public async Task ExecuteAsync(Character character, string certificationId)
    {
        var cert = character.Certifications.FirstOrDefault(c => c.Id == certificationId);
        if (cert is null) return;

        character.Certifications.Remove(cert);
        await _characterRepository.SaveAsync(character);
    }
}
