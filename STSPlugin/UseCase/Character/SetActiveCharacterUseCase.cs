using System;
using Sts.Domain;
using Sts.Domain.Character;

namespace STSPlugin.CharacterUseCases;

/// <summary>
/// Cas d'usage : définir le personnage actif.
/// Met à jour la configuration et synchronise le rang de l'engine STS.
/// Plugin-specific — reste synchrone (appelé depuis le render thread ImGui).
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

/// <summary>Implémentation par défaut de <see cref="SetActiveCharacterUseCase"/>.</summary>
public class DefaultSetActiveCharacterUseCase : SetActiveCharacterUseCase
{
    private readonly ICharacterRepository _repository;
    private readonly Configuration        _configuration;
    private readonly StsEngine            _engine;

    public DefaultSetActiveCharacterUseCase(
        ICharacterRepository repository,
        Configuration configuration,
        StsEngine engine)
    {
        _repository    = repository;
        _configuration = configuration;
        _engine        = engine;
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

        var character = _repository.GetByIdAsync(id.Value).GetAwaiter().GetResult();
        if (character is null) return;

        _configuration.ActiveCharacterId = character.Id;
        _configuration.Save();
        _engine.ChangeRank(character.RankKey);
    }
}
