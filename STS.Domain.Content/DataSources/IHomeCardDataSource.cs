using Sts.Domain.Content.Models;

namespace Sts.Domain.Content.DataSources;

/// <summary>Contrat d'accès brut aux données des cartes home.</summary>
public interface IHomeCardDataSource
{
    Task<IReadOnlyList<HomeCard>> LoadAsync();
    Task SaveAsync(IReadOnlyList<HomeCard> cards);
}
