using Sts.Domain.DataSource;
using Sts.Api.Services;

namespace Sts.Api.DataSources;

/// <summary>
/// Adaptateur IDataSource sur DataService.
/// Permet de réutiliser les Default*Repository du domain sans dupliquer le mapping.
/// </summary>
public sealed class DataServiceDataSource(DataService dataService) : IDataSource
{
    /// <inheritdoc/>
    public DataModel Load() => new()
    {
        Jobs = dataService.GetJobs(),
        Traits = dataService.GetTraits(),
        Abilities = dataService.GetAbilities(),
        Actions = dataService.GetActions(),
    };

    /// <inheritdoc/>
    public Task<DataModel> LoadAsync() => Task.FromResult(Load());
}
