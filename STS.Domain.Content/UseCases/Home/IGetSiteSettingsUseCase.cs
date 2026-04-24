using Sts.Domain.Content.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sts.Domain.Content.UseCases;

/// <summary>Retourne les paramètres éditoriaux du site.</summary>
public interface IGetSiteSettingsUseCase
{
    /// <returns>Les <see cref="SiteSettings"/> courants.</returns>
    Task<SiteSettings> ExecuteAsync();
}


/// <inheritdoc cref="IGetSiteSettingsUseCase"/>
public sealed class GetSiteSettingsUseCase(ISiteSettingsRepository repository) : IGetSiteSettingsUseCase
{
    /// <inheritdoc/>
    public Task<SiteSettings> ExecuteAsync() => repository.GetAsync();
}
