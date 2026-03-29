using STSPlugin.Domain;
using STSPlugin.Repository;
using System.Linq;

namespace STSPlugin.UseCases;

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
    Certification Execute(
        Character character,
        string name,
        string? linkedOriginTraitId = null,
        string? linkedAbilityId = null,
        int freePoints = 0);
}

public class DefaultAddCertificationUseCase : AddCertificationUseCase
{
    private readonly CharacterRepository _characterRepository;

    public DefaultAddCertificationUseCase(CharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    public Certification Execute(
        Character character,
        string name,
        string? linkedOriginTraitId = null,
        string? linkedAbilityId = null,
        int freePoints = 0)
    {
        var certification = new Certification
        {
            Name = name.Trim(),
            LinkedOriginTraitId = linkedOriginTraitId,
            LinkedAbilityId = linkedAbilityId,
            FreePoints = System.Math.Max(0, freePoints),
        };

        character.Certifications.Add(certification);
        _characterRepository.Save(character);
        return certification;
    }
}

/// <summary>
/// Cas d'usage : retirer une certification d'un personnage.
/// </summary>
public interface RemoveCertificationUseCase
{
    /// <summary>
    /// Retire la certification du personnage et persiste la modification.
    /// Si l'id est introuvable, l'opération est ignorée.
    /// </summary>
    void Execute(Character character, string certificationId);
}

public class DefaultRemoveCertificationUseCase : RemoveCertificationUseCase
{
    private readonly CharacterRepository _characterRepository;

    public DefaultRemoveCertificationUseCase(CharacterRepository characterRepository)
        => _characterRepository = characterRepository;

    public void Execute(Character character, string certificationId)
    {
        var cert = character.Certifications.FirstOrDefault(c => c.Id == certificationId);
        if (cert is null) return;

        character.Certifications.Remove(cert);
        _characterRepository.Save(character);
    }
}
