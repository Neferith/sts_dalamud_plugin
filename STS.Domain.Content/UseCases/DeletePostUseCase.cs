// IDeletePostUseCase.cs
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Supprime un post. Retourne <c>false</c> si section ou post introuvable.</summary>
public interface IDeletePostUseCase
{
    Task<bool> ExecuteAsync(string sectionId, string postId);
}

/// <inheritdoc cref="IDeletePostUseCase"/>
public sealed class DeletePostUseCase(IRulesRepository repository) : IDeletePostUseCase
{
    /// <inheritdoc/>
    public Task<bool> ExecuteAsync(string sectionId, string postId) =>
        repository.DeletePostAsync(sectionId, postId);
}
