using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sts.Domain;

namespace Sts.Domain.Character;

// ── GetAll ────────────────────────────────────────────────────────────────────

/// <summary>Cas d'usage : récupérer la liste complète des personnages.</summary>
public interface IGetAllCharactersUseCase
{
    /// <summary>Retourne tous les personnages, triés par nom.</summary>
    Task<IReadOnlyList<Character>> ExecuteAsync();
}

/// <summary>Implémentation par défaut de <see cref="IGetAllCharactersUseCase"/>.</summary>
public class GetAllCharactersUseCase : IGetAllCharactersUseCase
{
    private readonly ICharacterRepository _repository;

    /// <param name="repository">Repository de persistance des personnages.</param>
    public GetAllCharactersUseCase(ICharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public Task<IReadOnlyList<Character>> ExecuteAsync()
        => _repository.GetAllAsync();
}

// ── GetByUser ─────────────────────────────────────────────────────────────────

/// <summary>Cas d'usage : récupérer les personnages d'un utilisateur donné.</summary>
public interface IGetCharactersByUserUseCase
{
    /// <summary>
    /// Retourne les personnages appartenant à l'utilisateur <paramref name="userId"/>,
    /// triés par nom.
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur propriétaire.</param>
    Task<IReadOnlyList<Character>> ExecuteAsync(Guid userId);
}

/// <summary>Implémentation par défaut de <see cref="IGetCharactersByUserUseCase"/>.</summary>
public class GetCharactersByUserUseCase : IGetCharactersByUserUseCase
{
    private readonly ICharacterRepository _repository;

    /// <param name="repository">Repository de persistance des personnages.</param>
    public GetCharactersByUserUseCase(ICharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public Task<IReadOnlyList<Character>> ExecuteAsync(Guid userId)
        => _repository.GetByUserIdAsync(userId);
}

// ── GetById ───────────────────────────────────────────────────────────────────

/// <summary>Cas d'usage : récupérer un personnage par son identifiant.</summary>
public interface IGetCharacterByIdUseCase
{
    /// <summary>
    /// Retourne le personnage correspondant à <paramref name="id"/>,
    /// ou null s'il n'existe pas.
    /// </summary>
    /// <param name="id">Identifiant du personnage.</param>
    Task<Character?> ExecuteAsync(Guid id);
}

/// <summary>Implémentation par défaut de <see cref="IGetCharacterByIdUseCase"/>.</summary>
public class GetCharacterByIdUseCase : IGetCharacterByIdUseCase
{
    private readonly ICharacterRepository _repository;

    /// <param name="repository">Repository de persistance des personnages.</param>
    public GetCharacterByIdUseCase(ICharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public Task<Character?> ExecuteAsync(Guid id)
        => _repository.GetByIdAsync(id);
}

// ── Create ────────────────────────────────────────────────────────────────────

/// <summary>Cas d'usage : créer un nouveau personnage et le persister.</summary>
public interface ICreateCharacterUseCase
{
    /// <summary>
    /// Crée un personnage avec le nom, le rang et l'utilisateur propriétaire fournis,
    /// lui assigne un nouvel identifiant et le persiste immédiatement.
    /// </summary>
    /// <param name="name">Nom du personnage.</param>
    /// <param name="rank">Rang STS initial.</param>
    /// <param name="userId">
    /// Identifiant de l'utilisateur propriétaire.
    /// Null pour une création locale (plugin sans compte web).
    /// </param>
    /// <returns>Le personnage créé.</returns>
    Task<Character> ExecuteAsync(string name, RankKey rank, Guid? userId = null);
}

/// <summary>Implémentation par défaut de <see cref="ICreateCharacterUseCase"/>.</summary>
public class CreateCharacterUseCase : ICreateCharacterUseCase
{
    private readonly ICharacterRepository _repository;

    /// <param name="repository">Repository de persistance des personnages.</param>
    public CreateCharacterUseCase(ICharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public async Task<Character> ExecuteAsync(string name, RankKey rank, Guid? userId = null)
    {
        var character = new Character
        {
            Name   = name.Trim(),
            RankKey = rank,
            UserId = userId,
        };

        await _repository.SaveAsync(character);
        return character;
    }
}

// ── Update ────────────────────────────────────────────────────────────────────

/// <summary>Cas d'usage : mettre à jour un personnage existant.</summary>
public interface IUpdateCharacterUseCase
{
    /// <summary>
    /// Persiste les modifications apportées au personnage.
    /// L'identifiant et le <c>UserId</c> ne sont jamais modifiés par ce cas d'usage.
    /// </summary>
    /// <param name="character">Le personnage avec ses nouvelles valeurs.</param>
    Task ExecuteAsync(Character character);
}

/// <summary>Implémentation par défaut de <see cref="IUpdateCharacterUseCase"/>.</summary>
public class UpdateCharacterUseCase : IUpdateCharacterUseCase
{
    private readonly ICharacterRepository _repository;

    /// <param name="repository">Repository de persistance des personnages.</param>
    public UpdateCharacterUseCase(ICharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public async Task ExecuteAsync(Character character)
        => await _repository.SaveAsync(character);
}

// ── Delete ────────────────────────────────────────────────────────────────────

/// <summary>Cas d'usage : supprimer un personnage.</summary>
public interface IDeleteCharacterUseCase
{
    /// <summary>
    /// Supprime le personnage correspondant à <paramref name="id"/>.
    /// Sans effet si le personnage n'existe pas.
    /// </summary>
    /// <param name="id">Identifiant du personnage à supprimer.</param>
    Task ExecuteAsync(Guid id);
}

/// <summary>Implémentation par défaut de <see cref="IDeleteCharacterUseCase"/>.</summary>
public class DeleteCharacterUseCase : IDeleteCharacterUseCase
{
    private readonly ICharacterRepository _repository;

    /// <param name="repository">Repository de persistance des personnages.</param>
    public DeleteCharacterUseCase(ICharacterRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public Task ExecuteAsync(Guid id)
        => _repository.DeleteAsync(id);
}
