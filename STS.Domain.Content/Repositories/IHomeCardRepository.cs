using Sts.Domain.Content.Models;

namespace Sts.Domain.Content.Repositories;

public interface IHomeCardReadRepository
{
    Task<IReadOnlyList<HomeCard>> GetAllAsync();
}

/// <summary>Contrat de persistence des cartes home.</summary>
public interface IHomeCardRepository : IHomeCardReadRepository
{
    Task<HomeCard?> GetByIdAsync(Guid id);
    Task<HomeCard> CreateAsync(HomeCard card);
    Task<HomeCard?> UpdateAsync(HomeCard card);
    Task<bool> DeleteAsync(Guid id);
}
