// IUploadImageUseCase.cs
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>
/// Uploade une image.
/// Retourne l'URL complète si succès, un message d'erreur sinon.
/// </summary>
public interface IUploadImageUseCase
{
    Task<(string? url, string? error)> ExecuteAsync(string fileName, Stream stream, long sizeBytes);
}

/// <inheritdoc cref="IUploadImageUseCase"/>
public sealed class UploadImageUseCase(IImageRepository repository) : IUploadImageUseCase
{
    /// <inheritdoc/>
    public Task<(string? url, string? error)> ExecuteAsync(string fileName, Stream stream, long sizeBytes) =>
        repository.UploadAsync(fileName, stream, sizeBytes);
}
