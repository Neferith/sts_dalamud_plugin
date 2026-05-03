using Sts.Domain.User;

namespace Sts.Api.Auth;

/// <summary>
/// Implémentation de <see cref="IPasswordHasher"/> utilisant BCrypt.
/// Work factor 12 — adapté à un usage interactif.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <inheritdoc/>
    public string Hash(string plaintext)
        => BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor);

    /// <inheritdoc/>
    public bool Verify(string plaintext, string hash)
        => BCrypt.Net.BCrypt.Verify(plaintext, hash);
}
