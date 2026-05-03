namespace Sts.Domain.User;

/// <summary>Rôle d'un utilisateur dans le système STS.</summary>
public enum UserRole
{
    /// <summary>Membre de la guilde — peut créer et gérer ses propres fiches personnages.</summary>
    Member,

    /// <summary>Administrateur — accès complet à toutes les ressources.</summary>
    Admin,
}
