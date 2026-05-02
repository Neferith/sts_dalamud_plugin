using Sts.Domain;
using STSPlugin.Repository;
using Sts.Domain.Character;

namespace STSPlugin.legacy.CharacterUseCases;

/// <summary>
/// Cas d'usage : créer un nouveau personnage et le sauvegarder.
/// </summary>
public interface CreateCharacterUseCase
{
    /// <summary>
    /// Crée un personnage avec le nom et le rang fournis, lui assigne un nouvel identifiant
    /// et le persiste immédiatement.
    /// </summary>
    /// <param name="name">Nom du personnage.</param>
    /// <param name="rank">Rang STS initial.</param>
    /// <returns>Le personnage créé.</returns>
    Character Execute(string name, RankKey rank);
}

/// <summary>
/// Implémentation par défaut de <see cref="CreateCharacterUseCase"/>.
/// </summary>
public class DefaultCreateCharacterUseCase : CreateCharacterUseCase
{
    private readonly CharacterRepository _repository;

    public DefaultCreateCharacterUseCase(CharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public Character Execute(string name, RankKey rank)
    {
        var character = new Character
        {
            Name = name.Trim(),
            RankKey = rank,
        };

        _repository.Save(character);
        return character;
    }
}
