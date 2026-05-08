using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;
using System.Text.Json;

namespace STS.Api.Repositories;

/// <summary>Persistance du singleton <see cref="SiteSettings"/> dans un fichier JSON.</summary>
public sealed class SiteSettingsRepositoryOld : ISiteSettingsRepository
{
    private readonly string _filePath;
    private readonly ReaderWriterLockSlim _lock = new();
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    /// <param name="filePath">Chemin absolu vers <c>site-settings.json</c>.</param>
    public SiteSettingsRepositoryOld(string filePath) => _filePath = filePath;

    /// <inheritdoc/>
    public Task<SiteSettings> GetAsync()
    {
        _lock.EnterReadLock();
        try { return Task.FromResult(Read()); }
        finally { _lock.ExitReadLock(); }
    }

    /// <inheritdoc/>
    public Task<SiteSettings> SaveAsync(SiteSettings settings)
    {
        _lock.EnterWriteLock();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(settings, _json));
        }
        finally { _lock.ExitWriteLock(); }
        return Task.FromResult(settings);
    }

    private SiteSettings Read()
    {
        if (!File.Exists(_filePath)) return new SiteSettings();
        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<SiteSettings>(json, _json) ?? new SiteSettings();
    }
}
