using Sts.Domain;
using STSPlugin.Repository;

namespace STSPlugin.CharacterUseCases;

/// <summary>
/// Cas d'usage : récupérer le personnage actuellement actif.
/// </summary>
public interface GetActiveCharacterUseCase
{
    /// <summary>
    /// Retourne le personnage actif, ou null si aucun n'est sélectionné
    /// ou si l'identifiant sauvegardé ne correspond plus à un personnage existant.
    /// </summary>
    Character? Execute();
}

/// <summary>
/// Implémentation par défaut de <see cref="GetActiveCharacterUseCase"/>.
/// </summary>
public class DefaultGetActiveCharacterUseCase : GetActiveCharacterUseCase
{
    private readonly CharacterRepository _repository;
    private readonly Configuration _configuration;

    public DefaultGetActiveCharacterUseCase(CharacterRepository repository, Configuration configuration)
    {
        _repository = repository;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public Character? Execute()
        => _configuration.ActiveCharacterId is { } id
            ? _repository.GetById(id)
            : null;
}
