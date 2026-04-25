using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Crée un nouveau lien rapide.</summary>
public interface ICreateQuickLinkUseCase
{
    /// <param name="parameters">Données du lien à créer.</param>
    /// <returns>Le <see cref="QuickLink"/> créé.</returns>
    Task<QuickLink> ExecuteAsync(CreateQuickLinkParameters parameters);
}


/// <inheritdoc cref="ICreateQuickLinkUseCase"/>
public sealed class CreateQuickLinkUseCase(IQuickLinksRepository repository) : ICreateQuickLinkUseCase
{
    /// <inheritdoc/>
    public Task<QuickLink> ExecuteAsync(CreateQuickLinkParameters parameters)
    => repository.AddAsync(parameters);

}
