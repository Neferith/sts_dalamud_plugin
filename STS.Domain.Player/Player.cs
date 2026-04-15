namespace Sts.Domain.Player;

// ── Modèle ────────────────────────────────────────────────────────────────────

/// <summary>
/// Représente un compte joueur géré par l'administrateur.
/// Le code d'accès est stocké hashé — jamais en clair.
/// </summary>
public class Player
{
    /// <summary>Identifiant unique du joueur.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Nom d'utilisateur choisi par l'admin (ex : pseudo Discord, prénom).
    /// Unique dans le système, insensible à la casse.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Hash BCrypt du code d'accès généré par l'admin.
    /// Ne jamais exposer ce champ dans les réponses API.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Date de création du compte (UTC).</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

// ── IPasswordHasher ───────────────────────────────────────────────────────────

/// <summary>
/// Abstraction du mécanisme de hachage des codes d'accès joueur.
/// Permet de découpler le domain de toute librairie concrète (BCrypt, Argon2…).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Calcule le hash du <paramref name="plaintext"/> fourni.
    /// </summary>
    /// <param name="plaintext">Code en clair à hasher.</param>
    /// <returns>Le hash à stocker.</returns>
    string Hash(string plaintext);

    /// <summary>
    /// Vérifie qu'un code en clair correspond au hash stocké.
    /// </summary>
    /// <param name="plaintext">Code saisi par le joueur.</param>
    /// <param name="hash">Hash stocké.</param>
    /// <returns>True si le code est correct.</returns>
    bool Verify(string plaintext, string hash);
}

// ── IPlayerRepository ─────────────────────────────────────────────────────────

/// <summary>
/// Contrat d'accès aux comptes joueurs.
/// </summary>
public interface IPlayerRepository
{
    /// <summary>Retourne tous les joueurs, triés par nom d'utilisateur.</summary>
    Task<IReadOnlyList<Player>> GetAllAsync();

    /// <summary>
    /// Retourne un joueur par son identifiant, ou null s'il n'existe pas.
    /// </summary>
    /// <param name="id">Identifiant du joueur.</param>
    Task<Player?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retourne un joueur par son nom d'utilisateur, ou null s'il n'existe pas.
    /// Recherche insensible à la casse.
    /// </summary>
    /// <param name="username">Nom d'utilisateur.</param>
    Task<Player?> GetByUsernameAsync(string username);

    /// <summary>
    /// Crée un joueur.
    /// </summary>
    /// <param name="player">Le joueur à créer.</param>
    /// <returns>
    /// <c>true</c> si la création a réussi ;
    /// <c>false</c> si le nom d'utilisateur est déjà pris ;
    /// <c>null</c> en cas d'erreur interne.
    /// </returns>
    Task<bool?> CreateAsync(Player player);

    /// <summary>
    /// Remplace le hash du code d'accès d'un joueur existant.
    /// Sans effet si le joueur n'existe pas.
    /// </summary>
    /// <param name="id">Identifiant du joueur.</param>
    /// <param name="newPasswordHash">Nouveau hash à stocker.</param>
    Task UpdatePasswordHashAsync(Guid id, string newPasswordHash);

    /// <summary>
    /// Supprime un joueur par son identifiant.
    /// Sans effet si le joueur n'existe pas.
    /// </summary>
    /// <param name="id">Identifiant du joueur à supprimer.</param>
    Task DeleteAsync(Guid id);
}

// ── Use cases ─────────────────────────────────────────────────────────────────

/// <summary>Cas d'usage : récupérer la liste complète des joueurs.</summary>
public interface IGetAllPlayersUseCase
{
    /// <summary>Retourne tous les joueurs, triés par nom d'utilisateur.</summary>
    Task<IReadOnlyList<Player>> ExecuteAsync();
}

/// <summary>Implémentation par défaut de <see cref="IGetAllPlayersUseCase"/>.</summary>
public class GetAllPlayersUseCase : IGetAllPlayersUseCase
{
    private readonly IPlayerRepository _repository;

    /// <param name="repository">Repository de persistance des joueurs.</param>
    public GetAllPlayersUseCase(IPlayerRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public Task<IReadOnlyList<Player>> ExecuteAsync()
        => _repository.GetAllAsync();
}

/// <summary>Cas d'usage : créer un compte joueur.</summary>
public interface ICreatePlayerUseCase
{
    /// <summary>
    /// Crée un compte joueur avec le nom d'utilisateur et le code d'accès fournis.
    /// Le code est haché avant persistance.
    /// </summary>
    /// <param name="username">Nom d'utilisateur souhaité.</param>
    /// <param name="plainCode">Code d'accès en clair (sera haché).</param>
    /// <returns>
    /// Le joueur créé si la création a réussi ;
    /// <c>null</c> si le nom d'utilisateur est déjà pris.
    /// </returns>
    Task<Player?> ExecuteAsync(string username, string plainCode);
}

/// <summary>Implémentation par défaut de <see cref="ICreatePlayerUseCase"/>.</summary>
public class CreatePlayerUseCase : ICreatePlayerUseCase
{
    private readonly IPlayerRepository _repository;
    private readonly IPasswordHasher   _hasher;

    /// <param name="repository">Repository de persistance des joueurs.</param>
    /// <param name="hasher">Service de hachage des codes d'accès.</param>
    public CreatePlayerUseCase(IPlayerRepository repository, IPasswordHasher hasher)
    {
        _repository = repository;
        _hasher     = hasher;
    }

    /// <inheritdoc/>
    public async Task<Player?> ExecuteAsync(string username, string plainCode)
    {
        var player = new Player
        {
            Username     = username.Trim(),
            PasswordHash = _hasher.Hash(plainCode),
        };

        var result = await _repository.CreateAsync(player);
        return result == true ? player : null;
    }
}

/// <summary>
/// Cas d'usage : régénérer le code d'accès d'un joueur existant.
/// Appelé par l'admin (ex : joueur a perdu son code).
/// </summary>
public interface IUpdatePlayerCodeUseCase
{
    /// <summary>
    /// Remplace le code d'accès du joueur par <paramref name="newPlainCode"/>.
    /// </summary>
    /// <param name="playerId">Identifiant du joueur.</param>
    /// <param name="newPlainCode">Nouveau code en clair (sera haché).</param>
    Task ExecuteAsync(Guid playerId, string newPlainCode);
}

/// <summary>Implémentation par défaut de <see cref="IUpdatePlayerCodeUseCase"/>.</summary>
public class UpdatePlayerCodeUseCase : IUpdatePlayerCodeUseCase
{
    private readonly IPlayerRepository _repository;
    private readonly IPasswordHasher   _hasher;

    /// <param name="repository">Repository de persistance des joueurs.</param>
    /// <param name="hasher">Service de hachage des codes d'accès.</param>
    public UpdatePlayerCodeUseCase(IPlayerRepository repository, IPasswordHasher hasher)
    {
        _repository = repository;
        _hasher     = hasher;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync(Guid playerId, string newPlainCode)
        => _repository.UpdatePasswordHashAsync(playerId, _hasher.Hash(newPlainCode));
}

/// <summary>Cas d'usage : supprimer un compte joueur.</summary>
public interface IDeletePlayerUseCase
{
    /// <summary>
    /// Supprime le joueur correspondant à <paramref name="playerId"/>.
    /// Sans effet si le joueur n'existe pas.
    /// </summary>
    /// <param name="playerId">Identifiant du joueur à supprimer.</param>
    Task ExecuteAsync(Guid playerId);
}

/// <summary>Implémentation par défaut de <see cref="IDeletePlayerUseCase"/>.</summary>
public class DeletePlayerUseCase : IDeletePlayerUseCase
{
    private readonly IPlayerRepository _repository;

    /// <param name="repository">Repository de persistance des joueurs.</param>
    public DeletePlayerUseCase(IPlayerRepository repository)
        => _repository = repository;

    /// <inheritdoc/>
    public Task ExecuteAsync(Guid playerId)
        => _repository.DeleteAsync(playerId);
}

/// <summary>Cas d'usage : authentifier un joueur par son nom d'utilisateur et son code.</summary>
public interface IAuthenticatePlayerUseCase
{
    /// <summary>
    /// Vérifie les identifiants du joueur.
    /// </summary>
    /// <param name="username">Nom d'utilisateur saisi.</param>
    /// <param name="plainCode">Code d'accès en clair saisi.</param>
    /// <returns>
    /// Le <see cref="Player"/> authentifié si les identifiants sont corrects ;
    /// <c>null</c> si le joueur n'existe pas ou si le code est incorrect.
    /// </returns>
    Task<Player?> ExecuteAsync(string username, string plainCode);
}

/// <summary>Implémentation par défaut de <see cref="IAuthenticatePlayerUseCase"/>.</summary>
public class AuthenticatePlayerUseCase : IAuthenticatePlayerUseCase
{
    private readonly IPlayerRepository _repository;
    private readonly IPasswordHasher   _hasher;

    /// <param name="repository">Repository de persistance des joueurs.</param>
    /// <param name="hasher">Service de vérification des codes d'accès.</param>
    public AuthenticatePlayerUseCase(IPlayerRepository repository, IPasswordHasher hasher)
    {
        _repository = repository;
        _hasher     = hasher;
    }

    /// <inheritdoc/>
    public async Task<Player?> ExecuteAsync(string username, string plainCode)
    {
        var player = await _repository.GetByUsernameAsync(username);
        if (player is null)
            return null;

        return _hasher.Verify(plainCode, player.PasswordHash) ? player : null;
    }
}
