using System;
using STSPlugin.Repository;
using Sts.Domain.Character;

namespace STSPlugin.legacy.CharacterUseCases;

/// <summary>
/// Cas d'usage : supprimer un personnage.
/// Si le personnage supprimé était l'actif, l'identifiant actif est effacé de la configuration.
/// </summary>
public interface DeleteCharacterUseCase
{
    /// <summary>
    /// Supprime le personnage correspondant à l'identifiant fourni.
    /// </summary>
    /// <param name="id">Identifiant du personnage à supprimer.</param>
    void Execute(Guid id);
}

/// <summary>
/// Implémentation par défaut de <see cref="DeleteCharacterUseCase"/>.
/// </summary>
public class DefaultDeleteCharacterUseCase : DeleteCharacterUseCase
{
    private readonly CharacterRepository _repository;
    private readonly Configuration _configuration;

    public DefaultDeleteCharacterUseCase(CharacterRepository repository, Configuration configuration)
    {
        _repository = repository;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public void Execute(Guid id)
    {
        _repository.Delete(id);

        // Si le personnage supprimé était l'actif, on efface la référence
        if (_configuration.ActiveCharacterId == id)
        {
            _configuration.ActiveCharacterId = null;
            _configuration.Save();
        }
    }
}
