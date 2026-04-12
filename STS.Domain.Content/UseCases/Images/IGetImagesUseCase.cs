// IGetImagesUseCase.cs
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Retourne toutes les images uploadées.</summary>
public interface IGetImagesUseCase
{
    Task<IReadOnlyList<ImageInfo>> ExecuteAsync();
}

/// <inheritdoc cref="IGetImagesUseCase"/>
public sealed class GetImagesUseCase(IImageRepository repository) : IGetImagesUseCase
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<ImageInfo>> ExecuteAsync() =>
        repository.GetAllAsync();
}
