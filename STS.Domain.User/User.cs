using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sts.Domain.User;

// ── Modèle ────────────────────────────────────────────────────────────────────

/// <summary>
/// Représente un compte utilisateur du système STS.
/// Le code d'accès est stocké hashé — jamais en clair.
/// </summary>
public class User
{
    /// <summary>Identifiant unique de l'utilisateur.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Nom d'utilisateur unique dans le système (insensible à la casse).
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hash BCrypt du code d'accès.
    /// Ne jamais exposer ce champ dans les réponses API.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Rôle de l'utilisateur.</summary>
    public UserRole Role { get; set; } = UserRole.Member;

    /// <summary>Date de création du compte (UTC).</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

// ── IPasswordHasher ───────────────────────────────────────────────────────────

/// <summary>
/// Abstraction du mécanisme de hachage des codes d'accès.
/// Découple le domain de toute librairie concrète (BCrypt, Argon2…).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Calcule le hash du <paramref name="plaintext"/> fourni.</summary>
    /// <param name="plaintext">Code en clair à hasher.</param>
    /// <returns>Le hash à stocker.</returns>
    string Hash(string plaintext);

    /// <summary>Vérifie qu'un code en clair correspond au hash stocké.</summary>
    /// <param name="plaintext">Code saisi par l'utilisateur.</param>
    /// <param name="hash">Hash stocké.</param>
    /// <returns>True si le code est correct.</returns>
    bool Verify(string plaintext, string hash);
}

// ── IUserRepository ───────────────────────────────────────────────────────────

/// <summary>Contrat d'accès aux comptes utilisateurs.</summary>
public interface IUserRepository
{
    /// <summary>Retourne tous les utilisateurs, triés par nom d'utilisateur.</summary>
    Task<IReadOnlyList<User>> GetAllAsync();

    /// <summary>
    /// Retourne un utilisateur par son identifiant, ou null s'il n'existe pas.
    /// </summary>
    /// <param name="id">Identifiant de l'utilisateur.</param>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retourne un utilisateur par son nom d'utilisateur, ou null s'il n'existe pas.
    /// Recherche insensible à la casse.
    /// </summary>
    /// <param name="username">Nom d'utilisateur.</param>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// Crée un utilisateur.
    /// </summary>
    /// <param name="user">L'utilisateur à créer.</param>
    /// <returns>
    /// <c>true</c> si la création a réussi ;
    /// <c>false</c> si le nom d'utilisateur est déjà pris ;
    /// <c>null</c> en cas d'erreur interne.
    /// </returns>
    Task<bool?> CreateAsync(User user);

    /// <summary>
    /// Remplace le hash du code d'accès d'un utilisateur existant.
    /// Sans effet si l'utilisateur n'existe pas.
    /// </summary>
    /// <param name="id">Identifiant de l'utilisateur.</param>
    /// <param name="newPasswordHash">Nouveau hash à stocker.</param>
    Task UpdatePasswordHashAsync(Guid id, string newPasswordHash);

    /// <summary>
    /// Supprime un utilisateur par son identifiant.
    /// Sans effet si l'utilisateur n'existe pas.
    /// </summary>
    /// <param name="id">Identifiant de l'utilisateur à supprimer.</param>
    Task DeleteAsync(Guid id);
}

// ── Use cases ─────────────────────────────────────────────────────────────────

/// <summary>Cas d'usage : récupérer la liste complète des utilisateurs.</summary>
public interface IGetAllUsersUseCase
{
    /// <summary>Retourne tous les utilisateurs, triés par nom d'utilisateur.</summary>
    Task<IReadOnlyList<User>> ExecuteAsync();
}

/// <summary>Implémentation par défaut de <see cref="IGetAllUsersUseCase"/>.</summary>
public class GetAllUsersUseCase : IGetAllUsersUseCase
{
    private readonly IUserRepository _repository;

    /// <param name="repository">Repository de persistance des utilisateurs.</param>
    public GetAllUsersUseCase(IUserRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public Task<IReadOnlyList<User>> ExecuteAsync()
        => _repository.GetAllAsync();
}

/// <summary>Cas d'usage : créer un compte utilisateur.</summary>
public interface ICreateUserUseCase
{
    /// <summary>
    /// Crée un utilisateur avec le nom, le code et le rôle fournis.
    /// Le code est haché avant persistance.
    /// </summary>
    /// <param name="username">Nom d'utilisateur souhaité.</param>
    /// <param name="plainCode">Code d'accès en clair (sera haché).</param>
    /// <param name="role">Rôle de l'utilisateur.</param>
    /// <returns>
    /// L'utilisateur créé si la création a réussi ;
    /// <c>null</c> si le nom d'utilisateur est déjà pris.
    /// </returns>
    Task<User?> ExecuteAsync(string username, string plainCode, UserRole role = UserRole.Member);
}

/// <summary>Implémentation par défaut de <see cref="ICreateUserUseCase"/>.</summary>
public class CreateUserUseCase : ICreateUserUseCase
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _hasher;

    /// <param name="repository">Repository de persistance des utilisateurs.</param>
    /// <param name="hasher">Service de hachage des codes d'accès.</param>
    public CreateUserUseCase(IUserRepository repository, IPasswordHasher hasher)
    {
        _repository = repository;
        _hasher     = hasher;
    }

    /// <inheritdoc/>
    public async Task<User?> ExecuteAsync(string username, string plainCode, UserRole role = UserRole.Member)
    {
        var user = new User
        {
            Username     = username.Trim(),
            PasswordHash = _hasher.Hash(plainCode),
            Role         = role,
        };

        var result = await _repository.CreateAsync(user);
        return result == true ? user : null;
    }
}

/// <summary>
/// Cas d'usage : régénérer le code d'accès d'un utilisateur existant.
/// Appelé par l'admin.
/// </summary>
public interface IUpdateUserCodeUseCase
{
    /// <summary>
    /// Remplace le code d'accès de l'utilisateur par <paramref name="newPlainCode"/>.
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="newPlainCode">Nouveau code en clair (sera haché).</param>
    Task ExecuteAsync(Guid userId, string newPlainCode);
}

/// <summary>Implémentation par défaut de <see cref="IUpdateUserCodeUseCase"/>.</summary>
public class UpdateUserCodeUseCase : IUpdateUserCodeUseCase
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _hasher;

    /// <param name="repository">Repository de persistance des utilisateurs.</param>
    /// <param name="hasher">Service de hachage des codes d'accès.</param>
    public UpdateUserCodeUseCase(IUserRepository repository, IPasswordHasher hasher)
    {
        _repository = repository;
        _hasher     = hasher;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(Guid userId, string newPlainCode)
        => _repository.UpdatePasswordHashAsync(userId, _hasher.Hash(newPlainCode));
}

/// <summary>Cas d'usage : supprimer un compte utilisateur.</summary>
public interface IDeleteUserUseCase
{
    /// <summary>
    /// Supprime l'utilisateur correspondant à <paramref name="userId"/>.
    /// Sans effet si l'utilisateur n'existe pas.
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur à supprimer.</param>
    Task ExecuteAsync(Guid userId);
}

/// <summary>Implémentation par défaut de <see cref="IDeleteUserUseCase"/>.</summary>
public class DeleteUserUseCase : IDeleteUserUseCase
{
    private readonly IUserRepository _repository;

    /// <param name="repository">Repository de persistance des utilisateurs.</param>
    public DeleteUserUseCase(IUserRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public Task ExecuteAsync(Guid userId)
        => _repository.DeleteAsync(userId);
}

/// <summary>Cas d'usage : authentifier un utilisateur par son nom et son code.</summary>
public interface IAuthenticateUserUseCase
{
    /// <summary>
    /// Vérifie les identifiants de l'utilisateur.
    /// </summary>
    /// <param name="username">Nom d'utilisateur saisi.</param>
    /// <param name="plainCode">Code d'accès en clair saisi.</param>
    /// <returns>
    /// L'<see cref="User"/> authentifié si les identifiants sont corrects ;
    /// <c>null</c> si l'utilisateur n'existe pas ou si le code est incorrect.
    /// </returns>
    Task<User?> ExecuteAsync(string username, string plainCode);
}

/// <summary>Implémentation par défaut de <see cref="IAuthenticateUserUseCase"/>.</summary>
public class AuthenticateUserUseCase : IAuthenticateUserUseCase
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _hasher;

    /// <param name="repository">Repository de persistance des utilisateurs.</param>
    /// <param name="hasher">Service de vérification des codes d'accès.</param>
    public AuthenticateUserUseCase(IUserRepository repository, IPasswordHasher hasher)
    {
        _repository = repository;
        _hasher     = hasher;
    }

    /// <inheritdoc/>
    public async Task<User?> ExecuteAsync(string username, string plainCode)
    {
        var user = await _repository.GetByUsernameAsync(username);
        if (user is null) return null;

        return _hasher.Verify(plainCode, user.PasswordHash) ? user : null;
    }
}

/// <summary>
/// Cas d'usage : initialiser le compte admin depuis la configuration.
/// Crée le compte s'il n'existe pas encore en base.
/// À appeler au démarrage de l'application.
/// </summary>
public interface ISeedAdminUseCase
{
    /// <summary>
    /// Vérifie si un utilisateur avec le <paramref name="username"/> existe.
    /// S'il n'existe pas, le crée avec le rôle <see cref="UserRole.Admin"/>.
    /// </summary>
    /// <param name="username">Nom d'utilisateur admin depuis la configuration.</param>
    /// <param name="plainPassword">Mot de passe admin depuis la configuration.</param>
    Task ExecuteAsync(string username, string plainPassword);
}

/// <summary>Implémentation par défaut de <see cref="ISeedAdminUseCase"/>.</summary>
public class SeedAdminUseCase : ISeedAdminUseCase
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _hasher;

    /// <param name="repository">Repository de persistance des utilisateurs.</param>
    /// <param name="hasher">Service de hachage des codes d'accès.</param>
    public SeedAdminUseCase(IUserRepository repository, IPasswordHasher hasher)
    {
        _repository = repository;
        _hasher     = hasher;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(string username, string plainPassword)
    {
        var existing = await _repository.GetByUsernameAsync(username);
        if (existing is not null) return;

        var admin = new User
        {
            Username     = username.Trim(),
            PasswordHash = _hasher.Hash(plainPassword),
            Role         = UserRole.Admin,
        };

        await _repository.CreateAsync(admin);
    }
}
