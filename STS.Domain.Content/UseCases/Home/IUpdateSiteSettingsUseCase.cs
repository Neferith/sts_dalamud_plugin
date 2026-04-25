using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Met à jour les paramètres éditoriaux du site.</summary>
public interface IUpdateSiteSettingsUseCase
{
    /// <param name="settings">Nouvelles valeurs.</param>
    /// <returns>Les <see cref="SiteSettings"/> persistés.</returns>
    Task<SiteSettings> ExecuteAsync(SiteSettings settings);
}

/// <inheritdoc cref="IUpdateSiteSettingsUseCase"/>
public sealed class UpdateSiteSettingsUseCase(ISiteSettingsRepository repository) : IUpdateSiteSettingsUseCase
{
    /// <inheritdoc/>
    public async Task<SiteSettings> ExecuteAsync(SiteSettings settings)
    {
        await repository.SaveAsync(settings);
        return settings;
    }
}

