using System;
using STSPlugin.Repository;
using Sts.Domain;

namespace STSPlugin.CharacterUseCases;

/// <summary>
/// Cas d'usage : définir le personnage actif.
/// Met à jour la configuration et synchronise le rang de l'engine STS.
/// </summary>
public interface SetActiveCharacterUseCase
{
    /// <summary>
    /// Définit le personnage actif par son identifiant.
    /// Si l'identifiant est null, aucun personnage n'est actif.
    /// Si le personnage n'existe pas, l'opération est ignorée silencieusement.
    /// </summary>
    /// <param name="id">Identifiant du personnage à activer, ou null pour désélectionner.</param>
    void Execute(Guid? id);
}

/// <summary>
/// Implémentation par défaut de <see cref="SetActiveCharacterUseCase"/>.
/// </summary>
public class DefaultSetActiveCharacterUseCase : SetActiveCharacterUseCase
{
    private readonly CharacterRepository _repository;
    private readonly Configuration _configuration;
    private readonly StsEngine _engine;

    public DefaultSetActiveCharacterUseCase(
        CharacterRepository repository,
        Configuration configuration,
        StsEngine engine)
    {
        _repository = repository;
        _configuration = configuration;
        _engine = engine;
    }

    /// <inheritdoc/>
    public void Execute(Guid? id)
    {
        if (id is null)
        {
            _configuration.ActiveCharacterId = null;
            _configuration.Save();
            return;
        }

        var character = _repository.GetById(id.Value);
        if (character is null) return;

        // Persister l'actif
        _configuration.ActiveCharacterId = character.Id;
        _configuration.Save();

        // Synchroniser le rang dans l'engine
        _engine.ChangeRank(character.RankKey);
    }
}
