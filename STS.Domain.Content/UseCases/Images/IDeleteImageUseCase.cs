// IDeleteImageUseCase.cs
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Supprime une image. Retourne <c>false</c> si introuvable.</summary>
public interface IDeleteImageUseCase
{
    Task<bool> ExecuteAsync(string fileName);
}

/// <inheritdoc cref="IDeleteImageUseCase"/>
public sealed class DeleteImageUseCase(IImageRepository repository) : IDeleteImageUseCase
{
    /// <inheritdoc/>
    public Task<bool> ExecuteAsync(string fileName) =>
        repository.DeleteAsync(fileName);
}
