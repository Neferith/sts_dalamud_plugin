// IGetRulesUseCase.cs
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Retourne toutes les sections de règles.</summary>
public interface IGetRulesUseCase
{
    Task<IReadOnlyList<RulesSection>> ExecuteAsync();
}

/// <inheritdoc cref="IGetRulesUseCase"/>
public sealed class GetRulesUseCase(IRulesRepository repository) : IGetRulesUseCase
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<RulesSection>> ExecuteAsync() =>
        repository.GetAllAsync();
}
