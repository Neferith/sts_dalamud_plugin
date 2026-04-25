using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Met à jour un lien rapide existant.</summary>
public interface IUpdateQuickLinkUseCase
{
    /// <param name="id">Identifiant du lien à modifier.</param>
    /// <param name="parameters">Nouvelles valeurs.</param>
    /// <returns>Le <see cref="QuickLink"/> mis à jour, ou <see langword="null"/> s'il n'existe pas.</returns>
    Task<QuickLink?> ExecuteAsync(Guid id, UpdateQuickLinkParameters parameters);
}

/// <inheritdoc cref="IUpdateQuickLinkUseCase"/>
public sealed class UpdateQuickLinkUseCase(IQuickLinksRepository repository) : IUpdateQuickLinkUseCase
{
    /// <inheritdoc/>
    public async Task<QuickLink?> ExecuteAsync(Guid id, UpdateQuickLinkParameters parameters)
    => await repository.UpdateAsync(id, parameters);
}

