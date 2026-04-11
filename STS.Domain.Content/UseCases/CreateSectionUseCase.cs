// ICreateSectionUseCase.cs
using Sts.Domain.Content.Repositories;

namespace Sts.Domain.Content.UseCases;

/// <summary>Crée une nouvelle section. Retourne <c>false</c> si l'ID existe déjà.</summary>
public interface ICreateSectionUseCase
{
    Task<bool> ExecuteAsync(RulesSection section);
}

/// <inheritdoc cref="ICreateSectionUseCase"/>
public sealed class CreateSectionUseCase(IRulesRepository repository) : ICreateSectionUseCase
{
    /// <inheritdoc/>
    public Task<bool> ExecuteAsync(RulesSection section) =>
        repository.AddSectionAsync(section);
}
