// IUpdatePostUseCase.cs
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Met à jour un post. Retourne <c>false</c> si section ou post introuvable.</summary>
public interface IUpdatePostUseCase
{
    Task<bool> ExecuteAsync(string sectionId, string postId, string title, string content);
}


/// <inheritdoc cref="IUpdatePostUseCase"/>
public sealed class UpdatePostUseCase(IRulesRepository repository) : IUpdatePostUseCase
{
    /// <inheritdoc/>
    public Task<bool> ExecuteAsync(string sectionId, string postId, string title, string content) =>
        repository.UpdatePostAsync(sectionId, postId, title, content);
}
