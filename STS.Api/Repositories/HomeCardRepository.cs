using Sts.Domain.Content.DataSources;
using Sts.Domain.Content.Models;
using Sts.Domain.Content.Repositories;

namespace STS.Api.Repositories;

public sealed class HomeCardRepository : IHomeCardRepository
{
    private readonly IHomeCardDataSource _dataSource;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<HomeCard>? _cache;

    public HomeCardRepository(IHomeCardDataSource dataSource) => _dataSource = dataSource;

    private async Task<List<HomeCard>> LoadIfNeededAsync()
    {
        if (_cache is null)
        {
            var loaded = await _dataSource.LoadAsync();
            _cache = [.. loaded];
        }
        return _cache;
    }

    public async Task<IReadOnlyList<HomeCard>> GetAllAsync()
    {
        await _lock.WaitAsync();
        try { return await LoadIfNeededAsync(); }
        finally { _lock.Release(); }
    }

    public async Task<HomeCard?> GetByIdAsync(Guid id)
    {
        await _lock.WaitAsync();
        try { return (await LoadIfNeededAsync()).FirstOrDefault(c => c.Id == id); }
        finally { _lock.Release(); }
    }

    public async Task<HomeCard> CreateAsync(HomeCard card)
    {
        await _lock.WaitAsync();
        try
        {
            var cards = await LoadIfNeededAsync();
            cards.Add(card);
            await _dataSource.SaveAsync(cards);
            return card;
        }
        finally { _lock.Release(); }
    }

    public async Task<HomeCard?> UpdateAsync(HomeCard card)
    {
        await _lock.WaitAsync();
        try
        {
            var cards = await LoadIfNeededAsync();
            var idx = cards.FindIndex(c => c.Id == card.Id);
            if (idx < 0) return null;
            cards[idx] = card;
            await _dataSource.SaveAsync(cards);
            return card;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            var cards = await LoadIfNeededAsync();
            var removed = cards.RemoveAll(c => c.Id == id);
            if (removed > 0) await _dataSource.SaveAsync(cards);
            return removed > 0;
        }
        finally { _lock.Release(); }
    }
}
