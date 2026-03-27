using STSPlugin.Domain;
using STSPlugin.Repository;
using System.Collections.Generic;
using System.Linq;

namespace STSPlugin.UseCases;

/// <summary>
/// Cas d'usage : récupérer la liste complète des personnages sauvegardés.
/// </summary>
public interface GetAllCharactersUseCase
{
    /// <summary>Retourne tous les personnages, dans l'ordre de leur nom.</summary>
    IReadOnlyList<Character> Execute();
}

/// <summary>
/// Implémentation par défaut de <see cref="GetAllCharactersUseCase"/>.
/// </summary>
public class DefaultGetAllCharactersUseCase : GetAllCharactersUseCase
{
    private readonly CharacterRepository _repository;

    public DefaultGetAllCharactersUseCase(CharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public IReadOnlyList<Character> Execute()
        => [.. _repository.GetAll().OrderBy(c => c.Name)];
}
