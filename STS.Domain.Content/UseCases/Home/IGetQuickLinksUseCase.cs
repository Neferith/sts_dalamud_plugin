using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Retourne tous les liens rapides (usage admin).</summary>
public interface IGetQuickLinksUseCase
{
    /// <returns>Tous les <see cref="QuickLink"/>, triés par ordre.</returns>
    Task<IEnumerable<QuickLink>> ExecuteAsync();
}

/// <inheritdoc cref="IGetQuickLinksUseCase"/>
public sealed class GetQuickLinksUseCase(IQuickLinksRepository repository) : IGetQuickLinksUseCase
{
    /// <inheritdoc/>
    public async Task<IEnumerable<QuickLink>> ExecuteAsync()
        => (await repository.GetAllAsync()).OrderBy(l => l.Order);
}
