using Sts.Domain.User;

namespace Sts.Admin.Models;

/// <summary>Représentation d'un utilisateur retournée par l'API.</summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserDto Clone() => new()
    {
        Id = Id,
        Username = Username,
        Role = Role,
        CreatedAt = CreatedAt,
    };
}
