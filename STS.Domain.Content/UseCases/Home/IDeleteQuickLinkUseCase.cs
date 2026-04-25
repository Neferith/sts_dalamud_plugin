using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Supprime un lien rapide.</summary>
public interface IDeleteQuickLinkUseCase
{
    /// <param name="id">Identifiant du lien à supprimer.</param>
    /// <returns><see langword="true"/> si supprimé, <see langword="false"/> s'il n'existait pas.</returns>
    Task<bool> ExecuteAsync(Guid id);
}


/// <inheritdoc cref="IDeleteQuickLinkUseCase"/>
public sealed class DeleteQuickLinkUseCase(IQuickLinksRepository repository) : IDeleteQuickLinkUseCase
{
    /// <inheritdoc/>
    public Task<bool> ExecuteAsync(Guid id)
    => repository.DeleteAsync(id);
}
