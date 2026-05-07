using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;
using Sts.Domain.Content.UseCases;
using System.Text.Json;

namespace STS.Api.Repositories;

/// <summary>Persistance des <see cref="QuickLink"/> dans un fichier JSON.</summary>
public sealed class QuickLinksRepository : IQuickLinksRepository
{
    private readonly string _filePath;
    private readonly ReaderWriterLockSlim _lock = new();
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

    /// <param name="filePath">Chemin absolu vers <c>quick-links.json</c>.</param>
    public QuickLinksRepository(string filePath) => _filePath = filePath;

    /// <inheritdoc/>
    public Task<IEnumerable<QuickLink>> GetAllAsync()
    {
        _lock.EnterReadLock();
        try { return Task.FromResult(ReadAll()); }
        finally { _lock.ExitReadLock(); }
    }

    /// <inheritdoc/>
    public Task<QuickLink?> GetByIdAsync(Guid id)
    {
        _lock.EnterReadLock();
        try { return Task.FromResult(ReadAll().FirstOrDefault(l => l.Id == id)); }
        finally { _lock.ExitReadLock(); }
    }

    /// <inheritdoc/>
    public Task<QuickLink> AddAsync(CreateQuickLinkParameters parameters)
    {
        _lock.EnterWriteLock();
        try
        {
            var list = ReadAll().ToList();
            var link = new QuickLink
            {
                Id = Guid.NewGuid(),
                Label = parameters.Label,
                Url = parameters.Url,
                Icon = parameters.Icon,
                Category = parameters.Category,
                Order = parameters.Order,
                IsVisible = parameters.IsVisible,
            };
            list.Add(link);
            WriteAll(list);
            return Task.FromResult(link);
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <inheritdoc/>
    public Task<QuickLink?> UpdateAsync(Guid id, UpdateQuickLinkParameters parameters)
    {
        _lock.EnterWriteLock();
        try
        {
            var list = ReadAll().ToList();
            var idx = list.FindIndex(l => l.Id == id);
            if (idx < 0) return Task.FromResult<QuickLink?>(null);

            var updated = list[idx] with
            {
                Label = parameters.Label,
                Url = parameters.Url,
                Icon = parameters.Icon,
                Category = parameters.Category,
                Order = parameters.Order,
                IsVisible = parameters.IsVisible,
            };
            list[idx] = updated;
            WriteAll(list);
            return Task.FromResult<QuickLink?>(updated);
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(Guid id)
    {
        _lock.EnterWriteLock();
        try
        {
            var list = ReadAll().ToList();
            var initial = list.Count;
            list.RemoveAll(l => l.Id == id);
            if (list.Count == initial) return Task.FromResult(false);
            WriteAll(list);
            return Task.FromResult(true);
        }
        finally { _lock.ExitWriteLock(); }
    }

    private IEnumerable<QuickLink> ReadAll()
    {
        if (!File.Exists(_filePath)) return [];
        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<QuickLink>>(json, _json) ?? [];
    }

    private void WriteAll(IEnumerable<QuickLink> links)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(links, _json));
    }
}
