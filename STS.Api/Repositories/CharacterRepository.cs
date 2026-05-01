using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sts.Domain.Character;

namespace Sts.Api.Repositories;

/// <summary>
/// Implémentation JSON de <see cref="ICharacterRepository"/>.
/// Persiste tous les personnages dans un seul fichier <c>characters.json</c>.
/// Thread-safe via <see cref="SemaphoreSlim"/>.
/// </summary>
public class CharacterRepository : ICharacterRepository
{
    private readonly string           _filePath;
    private readonly SemaphoreSlim    _lock = new(1, 1);
    private          List<Character>? _cache;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <param name="filePath">Chemin absolu vers <c>characters.json</c>.</param>
    public CharacterRepository(string filePath) => _filePath = filePath;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Character>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return [.. _cache!.OrderBy(c => c.Name)];
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Character>> GetByUserIdAsync(Guid userId)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return [.. _cache!.Where(c => c.UserId == userId).OrderBy(c => c.Name)];
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task<Character?> GetByIdAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            return _cache!.FirstOrDefault(c => c.Id == id);
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(Character character)
    {
        await _lock.WaitAsync();
        try
        {
            await EnsureLoadedAsync();
            var idx = _cache!.FindIndex(c => c.Id == character.Id);
            if (idx >= 0) _cache[idx] = character;
            else          _cache.Add(character);
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
            _cache!.RemoveAll(c => c.Id == id);
            await PersistAsync();
        }
        finally { _lock.Release(); }
    }

    // ── Privé ─────────────────────────────────────────────────────────────────

    private async Task EnsureLoadedAsync()
    {
        if (_cache is not null) return;

        if (!File.Exists(_filePath))
        {
            _cache = [];
            return;
        }

        var json = await File.ReadAllTextAsync(_filePath);
        _cache = JsonSerializer.Deserialize<List<Character>>(json, JsonOptions) ?? [];
    }

    private async Task PersistAsync()
    {
        var json = JsonSerializer.Serialize(_cache, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
