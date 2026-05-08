// IGetHomeCardsUseCase.cs (admin — toutes)
using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;

public interface IGetHomeCardsUseCase
{
    Task<IReadOnlyList<HomeCard>> ExecuteAsync();
}

// IGetVisibleHomeCardsUseCase.cs (public — visibles + triées)
public interface IGetVisibleHomeCardsUseCase
{
    Task<IReadOnlyList<HomeCard>> ExecuteAsync();
}

// ICreateHomeCardUseCase.cs
public interface ICreateHomeCardUseCase
{
    Task<HomeCard> ExecuteAsync(HomeCard card);
}

// IUpdateHomeCardUseCase.cs
public interface IUpdateHomeCardUseCase
{
    Task<HomeCard?> ExecuteAsync(HomeCard card);
}

// IDeleteHomeCardUseCase.cs
public interface IDeleteHomeCardUseCase
{
    Task<bool> ExecuteAsync(Guid id);
}



// GetHomeCardsUseCase.cs
public sealed class GetHomeCardsUseCase(IHomeCardReadRepository repository) : IGetHomeCardsUseCase
{
    public Task<IReadOnlyList<HomeCard>> ExecuteAsync() => repository.GetAllAsync();
}

// GetVisibleHomeCardsUseCase.cs
public sealed class GetVisibleHomeCardsUseCase(IHomeCardReadRepository repository) : IGetVisibleHomeCardsUseCase
{
    public async Task<IReadOnlyList<HomeCard>> ExecuteAsync()
    {
        var all = await repository.GetAllAsync();
        return all.Where(c => c.IsVisible).OrderBy(c => c.Order).ToList();
    }
}

// CreateHomeCardUseCase.cs
public sealed class CreateHomeCardUseCase(IHomeCardRepository repository) : ICreateHomeCardUseCase
{
    public Task<HomeCard> ExecuteAsync(HomeCard card) => repository.CreateAsync(card);
}

// UpdateHomeCardUseCase.cs
public sealed class UpdateHomeCardUseCase(IHomeCardRepository repository) : IUpdateHomeCardUseCase
{
    public Task<HomeCard?> ExecuteAsync(HomeCard card) => repository.UpdateAsync(card);
}

// DeleteHomeCardUseCase.cs
public sealed class DeleteHomeCardUseCase(IHomeCardRepository repository) : IDeleteHomeCardUseCase
{
    public Task<bool> ExecuteAsync(Guid id) => repository.DeleteAsync(id);
}
