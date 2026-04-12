using Sts.Domain.Content;
using Sts.Domain.Content.DataSources;
using Sts.Domain.Content.Repositories;

namespace Sts.Api.Repositories;

/// <summary>
/// Implémentation de <see cref="IRulesRepository"/> avec cache mémoire et persistence via <see cref="IRulesDataSource"/>.
/// Thread-safe via <see cref="SemaphoreSlim"/> — registré en singleton.
/// </summary>
public sealed class RulesRepository : IRulesRepository
{
    private readonly IRulesDataSource _dataSource;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<RulesSection>? _sections;

    /// <summary>Initialise le repository avec la source de données fournie.</summary>
    public RulesRepository(IRulesDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // ── Interne — appelé depuis l'intérieur du lock uniquement ────────────────

    /// <summary>
    /// Charge les sections si ce n'est pas encore fait.
    /// Doit être appelé depuis l'intérieur du lock.
    /// </summary>
    private async Task<List<RulesSection>> GetSectionsAsync()
    {
        if (_sections is null)
        {
            var loaded = await _dataSource.LoadAsync();
            _sections = [.. loaded.OrderBy(s => s.Order)];
        }
        return _sections;
    }

    // ── Lecture ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RulesSection>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try { return await GetSectionsAsync(); }
        finally { _lock.Release(); }
    }

    // ── Sections ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<bool> AddSectionAsync(RulesSection section)
    {
        await _lock.WaitAsync();
        try
        {
            var sections = await GetSectionsAsync();
            if (sections.Any(s => s.Id == section.Id)) return false;

            _sections = [.. sections.Append(section).OrderBy(s => s.Order)];
            await _dataSource.SaveAsync(_sections);
            return true;
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateSectionAsync(string sectionId, string title, int order)
    {
        await _lock.WaitAsync();
        try
        {
            var sections = await GetSectionsAsync();
            var existing = sections.FirstOrDefault(s => s.Id == sectionId);
            if (existing is null) return false;

            var updated = new RulesSection
            {
                Id = existing.Id,
                Title = title,
                Order = order,
                Posts = existing.Posts,
            };

            _sections = [.. sections.Select(s => s.Id == sectionId ? updated : s)
                                    .OrderBy(s => s.Order)];
            await _dataSource.SaveAsync(_sections);
            return true;
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSectionAsync(string sectionId)
    {
        await _lock.WaitAsync();
        try
        {
            var sections = await GetSectionsAsync();
            if (!sections.Any(s => s.Id == sectionId)) return false;

            _sections = sections.Where(s => s.Id != sectionId).ToList();
            await _dataSource.SaveAsync(_sections);
            return true;
        }
        finally { _lock.Release(); }
    }

    // ── Posts ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<bool?> AddPostAsync(string sectionId, RulesPost post)
    {
        await _lock.WaitAsync();
        try
        {
            var sections = await GetSectionsAsync();
            var section = sections.FirstOrDefault(s => s.Id == sectionId);
            if (section is null) return null;
            if (section.Posts.Any(p => p.Id == post.Id)) return false;

            var updated = new RulesSection
            {
                Id = section.Id,
                Title = section.Title,
                Order = section.Order,
                Posts = [.. section.Posts, post],
            };

            _sections = [.. sections.Select(s => s.Id == sectionId ? updated : s)];
            await _dataSource.SaveAsync(_sections);
            return true;
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdatePostAsync(string sectionId, string postId, string title, string content)
    {
        await _lock.WaitAsync();
        try
        {
            var sections = await GetSectionsAsync();
            var section = sections.FirstOrDefault(s => s.Id == sectionId);
            if (section is null) return false;
            if (!section.Posts.Any(p => p.Id == postId)) return false;

            var updated = new RulesSection
            {
                Id = section.Id,
                Title = section.Title,
                Order = section.Order,
                Posts = section.Posts
                    .Select(p => p.Id == postId
                        ? new RulesPost { Id = postId, Title = title, Content = content }
                        : p)
                    .ToList(),
            };

            _sections = [.. sections.Select(s => s.Id == sectionId ? updated : s)];
            await _dataSource.SaveAsync(_sections);
            return true;
        }
        finally { _lock.Release(); }
    }

    /// <inheritdoc/>
    public async Task<bool> DeletePostAsync(string sectionId, string postId)
    {
        await _lock.WaitAsync();
        try
        {
            var sections = await GetSectionsAsync();
            var section = sections.FirstOrDefault(s => s.Id == sectionId);
            if (section is null) return false;
            if (!section.Posts.Any(p => p.Id == postId)) return false;

            var updated = new RulesSection
            {
                Id = section.Id,
                Title = section.Title,
                Order = section.Order,
                Posts = section.Posts.Where(p => p.Id != postId).ToList(),
            };

            _sections = [.. sections.Select(s => s.Id == sectionId ? updated : s)];
            await _dataSource.SaveAsync(_sections);
            return true;
        }
        finally { _lock.Release(); }
    }
}
