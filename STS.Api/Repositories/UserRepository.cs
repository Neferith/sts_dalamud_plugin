using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sts.Domain.User;

namespace Sts.Api.Repositories;

/// <summary>
/// Implémentation JSON de <see cref="IUserRepository"/>.
/// Persiste tous les utilisateurs dans un seul fichier <c>users.json</c>.
/// Thread-safe via <see cref="SemaphoreSlim"/>.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly string      _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private          List<User>? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <param name="filePath">Chemin absolu vers <c>users.json</c>.</param>
    public UserRepository(string filePath) => _filePath = filePath;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return [.. _cache!.OrderBy(u => u.Username)];
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task<User?> GetByIdAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return _cache!.FirstOrDefault(u => u.Id == id);
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return _cache!.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task<bool?> CreateAsync(User user)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();

            var conflict = _cache!.Any(u =>
                string.Equals(u.Username, user.Username, StringComparison.OrdinalIgnoreCase));

            if (conflict) return false;

            _cache.Add(user);
            await PersistAsync();
            return true;
        }
        catch { return null; }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task UpdatePasswordHashAsync(Guid id, string newPasswordHash)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            var user = _cache!.FirstOrDefault(u => u.Id == id);
            if (user is null) return;

            user.PasswordHash = newPasswordHash;
            await PersistAsync();
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            _cache!.RemoveAll(u => u.Id == id);
            await PersistAsync();
        }
        finally { _lock.Release(); }
    }

    // ── Privé ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Charge le fichier JSON si le cache n'est pas encore initialisé.
    /// Doit être appelé uniquement depuis une section protégée par <c>_lock</c>.
    /// </summary>
    private async Task EnsureLoadedAsync()
    {
        if (_cache is not null) return;

        if (!File.Exists(_filePath))
        {
            _cache = [];
            return;
        }

        var json = await File.ReadAllTextAsync(_filePath);
        _cache = JsonSerializer.Deserialize<List<User>>(json, JsonOptions) ?? [];
    }

    /// <summary>
    /// Sérialise le cache vers le fichier.
    /// Doit être appelé uniquement depuis une section protégée par <c>_lock</c>.
    /// </summary>
    private async Task PersistAsync()
    {
        var json = JsonSerializer.Serialize(_cache, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
