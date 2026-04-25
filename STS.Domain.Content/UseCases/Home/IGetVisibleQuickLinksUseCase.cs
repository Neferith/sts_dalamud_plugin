using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Retourne les liens rapides visibles (usage home publique).</summary>
public interface IGetVisibleQuickLinksUseCase
{
    /// <returns>Les <see cref="QuickLink"/> avec <see cref="QuickLink.IsVisible"/> à <see langword="true"/>, triés par ordre.</returns>
    Task<IEnumerable<QuickLink>> ExecuteAsync();
}

/// <inheritdoc cref="IGetVisibleQuickLinksUseCase"/>
public sealed class GetVisibleQuickLinksUseCase(IQuickLinksReadRepository repository) : IGetVisibleQuickLinksUseCase
{
    /// <inheritdoc/>
    public async Task<IEnumerable<QuickLink>> ExecuteAsync()
        => (await repository.GetAllAsync())
            .Where(l => l.IsVisible)
            .OrderBy(l => l.Category)
            .ThenBy(l => l.Order);
}
