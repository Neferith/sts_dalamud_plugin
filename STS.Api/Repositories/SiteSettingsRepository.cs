using Sts.Domain.Content.DataSources;
using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;

namespace STS.Api.Repositories;

public class SiteSettingsRepository: ISiteSettingsRepository
{
    private readonly ISiteSettingsDataSource _dataSource;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private SiteSettings? _siteSettings;

    /// <summary>Initialise le repository avec la source de données fournie.</summary>
    public SiteSettingsRepository(ISiteSettingsDataSource dataSource)
    {
        _dataSource = dataSource;
    }


    // ── Interne — appelé depuis l'intérieur du lock uniquement ────────────────

    /// <summary>
    /// Charge les settings du site si ce n'est pas encore fait.
    /// Doit être appelé depuis l'intérieur du lock.
    /// </summary>
    private async Task<SiteSettings> LoadIfNeededAsync()
    {
        if (_siteSettings is null)
        {
            var loaded = await _dataSource.LoadAsync();
            _siteSettings = loaded;
        }
        return _siteSettings;
    }

    // ── Lecture ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<SiteSettings> GetAsync()
    {
        await _lock.WaitAsync();
        try { return (await LoadIfNeededAsync()); }
        finally { _lock.Release(); }
    }

    // ── Écriture ─────────────────────────────────────────────────────────────

    public async Task<SiteSettings> SaveAsync(SiteSettings settings)
    {
        await _lock.WaitAsync();
        try
        {
            await _dataSource.SaveAsync(settings);
            _siteSettings = settings;
            return settings;
        }
        finally { _lock.Release(); }
    }

}

