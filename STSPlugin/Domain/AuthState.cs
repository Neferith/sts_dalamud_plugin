using System;

namespace STSPlugin.Auth;

/// <summary>
/// État d'authentification partagé entre les use cases auth du plugin.
/// Singleton — créé une seule fois dans <see cref="MainDiContainer"/>.
/// </summary>
public class AuthState
{
    /// <summary>JWT en mémoire. Null si non connecté.</summary>
    public string? Token { get; set; }

    /// <summary>Date d'expiration du token (UTC). MinValue si non connecté.</summary>
    public DateTime TokenExpiry { get; set; } = DateTime.MinValue;

    /// <summary>Nom d'utilisateur authentifié. Null si non connecté.</summary>
    public string? Username { get; set; }

    /// <summary>Identifiant de l'utilisateur connecté. Null si non connecté.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Dernière erreur de connexion. Null si aucune.</summary>
    public string? LastError { get; set; }

    /// <summary>True si un token valide est en mémoire.</summary>
    public bool IsAuthenticated => Token is not null && DateTime.UtcNow < TokenExpiry;

    /// <summary>Déclenché à chaque changement d'état (login/logout).</summary>
    public event Action? OnAuthChanged;

    /// <summary>Notifie les abonnés d'un changement d'état.</summary>
    public void Notify() => OnAuthChanged?.Invoke();
}
