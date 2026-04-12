// ICreatePostUseCase.cs
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>
/// Crée un post dans une section.
/// <c>true</c> = créé ; <c>false</c> = ID en conflit ; <c>null</c> = section introuvable.
/// </summary>
public interface ICreatePostUseCase
{
    Task<bool?> ExecuteAsync(string sectionId, RulesPost post);
}

/// <inheritdoc cref="ICreatePostUseCase"/>
public sealed class CreatePostUseCase(IRulesRepository repository) : ICreatePostUseCase
{
    /// <inheritdoc/>
    public Task<bool?> ExecuteAsync(string sectionId, RulesPost post) =>
        repository.AddPostAsync(sectionId, post);
}
