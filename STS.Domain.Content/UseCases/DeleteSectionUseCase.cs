// IDeleteSectionUseCase.cs
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Supprime une section et tous ses posts. Retourne <c>false</c> si introuvable.</summary>
public interface IDeleteSectionUseCase
{
    Task<bool> ExecuteAsync(string sectionId);
}

/// <inheritdoc cref="IDeleteSectionUseCase"/>
public sealed class DeleteSectionUseCase(IRulesRepository repository) : IDeleteSectionUseCase
{
    /// <inheritdoc/>
    public Task<bool> ExecuteAsync(string sectionId) =>
        repository.DeleteSectionAsync(sectionId);
}
