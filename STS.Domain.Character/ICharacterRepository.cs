namespace Sts.Domain.Character;

/// <summary>
/// Contrat d'accès aux fiches personnages.
/// </summary>
public interface ICharacterRepository
{
    /// <summary>Retourne tous les personnages, triés par nom.</summary>
    Task<IReadOnlyList<Character>> GetAllAsync();

    /// <summary>
    /// Retourne tous les personnages appartenant à un joueur donné, triés par nom.
    /// </summary>
    /// <param name="playerId">Identifiant du joueur propriétaire.</param>
    Task<IReadOnlyList<Character>> GetByPlayerIdAsync(Guid playerId);

    /// <summary>
    /// Retourne un personnage par son identifiant, ou null s'il n'existe pas.
    /// </summary>
    /// <param name="id">Identifiant du personnage.</param>
    Task<Character?> GetByIdAsync(Guid id);

    /// <summary>Sauvegarde un personnage (création ou mise à jour).</summary>
    /// <param name="character">Le personnage à persister.</param>
    Task SaveAsync(Character character);

    /// <summary>
    /// Supprime un personnage par son identifiant.
    /// Sans effet si le personnage n'existe pas.
    /// </summary>
    /// <param name="id">Identifiant du personnage à supprimer.</param>
    Task DeleteAsync(Guid id);
}
