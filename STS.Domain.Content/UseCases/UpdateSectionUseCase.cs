// IUpdateSectionUseCase.cs
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Met à jour titre et ordre d'une section. Retourne <c>false</c> si introuvable.</summary>
public interface IUpdateSectionUseCase
{
    Task<bool> ExecuteAsync(string sectionId, string title, int order);
}

/// <inheritdoc cref="IUpdateSectionUseCase"/>
public sealed class UpdateSectionUseCase(IRulesRepository repository) : IUpdateSectionUseCase
{
    /// <inheritdoc/>
    public Task<bool> ExecuteAsync(string sectionId, string title, int order) =>
        repository.UpdateSectionAsync(sectionId, title, order);
}
